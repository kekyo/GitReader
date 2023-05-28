////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Threading;

namespace GitReader.Internal;

internal struct BufferPoolBuffer : IDisposable
{
    private byte[] buffer;

    internal BufferPoolBuffer(byte[] buffer)
    {
        this.buffer = buffer;
        this.Length = buffer.Length;
    }

    public void Dispose() =>
        BufferPool.Release(ref this.buffer);

    public int Length { get; }

    public byte this[int index]
    {
        get => this.buffer[index];
        set => this.buffer[index] = value;
    }

    public DetachedBufferPoolBuffer Detach() =>
        new(Interlocked.Exchange(ref this.buffer, null!));

    public static implicit operator BufferPoolBuffer(byte[] buffer) =>
        new(buffer);
    public static implicit operator byte[](BufferPoolBuffer buffer) =>
        buffer.buffer;
}

internal readonly struct DetachedBufferPoolBuffer
{
    private readonly byte[] buffer;

    internal DetachedBufferPoolBuffer(byte[] buffer) =>
        this.buffer = buffer;

    public static implicit operator DetachedBufferPoolBuffer(byte[] buffer) =>
        new(buffer);
    public static implicit operator BufferPoolBuffer(DetachedBufferPoolBuffer buffer) =>
        new(buffer.buffer);
}

internal static class BufferPool
{
    private sealed class BufferHolder
    {
        private volatile BufferHolder? next;
        private readonly WeakReference bwr;

        public BufferHolder() =>
            this.bwr = new(null);

        private BufferHolder(byte[] buffer) =>
            this.bwr = new(buffer);

        public byte[] Take(int size)
        {
            var current = this;
            do
            {
                if (current.bwr.Target is byte[] buffer &&
                    buffer.Length == size)
                {
                    lock (current.bwr)
                    {
                        if (current.bwr.Target is byte[] b &&
                            b.Length == size)
                        {
                            current.bwr.Target = null;
                            return b;
                        }
                    }
                }
                current = current.next;
            }
            while (current != null);

            return new byte[size];
        }

        public void Release(byte[] buffer)
        {
            var current = this;
            BufferHolder last;
            do
            {
                if (current.bwr.Target == null)
                {
                    lock (current.bwr)
                    {
                        if (current.bwr.Target == null)
                        {
                            current.bwr.Target = buffer;
                            return;
                        }
                    }
                }
                last = current;
                current = current.next;
            }
            while (current != null);

            var next = new BufferHolder(buffer);

            while (true)
            {
                if (Interlocked.CompareExchange(ref last.next, next, null) is not { } cnext)
                {
                    return;
                }
                last = cnext;
            }
        }
    }

    private static readonly BufferHolder[] bufferHolders = new[]
    {
        new BufferHolder(),  // 0 - 12
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
        new BufferHolder(),
    };

    static BufferPool()
    {
        Debug.Assert(bufferHolders.Length == 13);
    }

    public static BufferPoolBuffer Take(int size)
    {
        var bufferHolder = bufferHolders[size % 13];
        return bufferHolder.Take(size);
    }

    internal static void Release(ref byte[] buffer)
    {
        if (Interlocked.Exchange(ref buffer, null!) is { } b)
        {
            var bufferHolder = bufferHolders[b.Length % 13];
            bufferHolder.Release(b);
        }
    }
}
