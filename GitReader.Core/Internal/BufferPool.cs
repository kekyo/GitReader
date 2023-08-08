﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace GitReader.Internal;

[DebuggerStepThrough]
internal struct BufferPoolBuffer : IDisposable
{
    private readonly BufferPool? pool; 
    private byte[] buffer;

    internal BufferPoolBuffer(BufferPool? pool, byte[] buffer)
    {
        this.pool = pool;
        this.buffer = buffer;
        this.Length = buffer.Length;
    }

    public void Dispose() =>
        this.pool?.Release(ref this.buffer);

    public int Length { get; }

    public byte this[int index]
    {
        get => this.buffer[index];
        set => this.buffer[index] = value;
    }

    public DetachedBufferPoolBuffer Detach() =>
        new(this.pool, Interlocked.Exchange(ref this.buffer, null!));

    public static implicit operator byte[](BufferPoolBuffer buffer) =>
        buffer.buffer;
}

[DebuggerStepThrough]
internal readonly struct DetachedBufferPoolBuffer
{
    private readonly BufferPool? pool;
    private readonly byte[] buffer;

    internal DetachedBufferPoolBuffer(BufferPool? pool, byte[] buffer)
    {
        this.pool = pool;
        this.buffer = buffer;
    }

    public static implicit operator BufferPoolBuffer(DetachedBufferPoolBuffer buffer) =>
        new(buffer.pool, buffer.buffer);
}

[DebuggerStepThrough]
internal sealed class BufferPool
{
    // Tried and tested, but simple strategies were the fastest.
    // Probably because each buffer table and lookup fragments on the CPU cache.

    private const int MaxReservedBufferElements = 32;
    private const int BufferHolders = 13;

    [DebuggerStepThrough]
    private sealed class BufferHolder
    {
        private readonly byte[]?[] buffers = new byte[MaxReservedBufferElements][];

        public byte[] Take(int size)
        {
            for (var index = 0; index < MaxReservedBufferElements; index++)
            {
                var buffer = this.buffers[index];
                if (buffer != null && buffer.Length == size)
                {
                    if (Interlocked.CompareExchange(ref this.buffers[index], null, buffer) == buffer)
                    {
                        return buffer!;
                    }
                }
            }

            return new byte[size];
        }

        public void Release(byte[] buffer)
        {
            for (var index = 0; index < MaxReservedBufferElements; index++)
            {
                if (this.buffers[index] == null)
                {
                    if (Interlocked.CompareExchange(ref this.buffers[index], buffer, null) == null)
                    {
                        break;
                    }
                }
            }

            // It was better to simply discard a buffer instance than the cost of extending the table.
        }
    }

    private readonly BufferHolder[] bufferHolders;

    public BufferPool() =>
        bufferHolders = Enumerable.Range(0, BufferHolders).
        Select(_ => new BufferHolder()).
        ToArray();

    public BufferPoolBuffer Take(int size)
    {
        var bufferHolder = this.bufferHolders[size % BufferHolders];
        return new(this, bufferHolder.Take(size));
    }

    internal void Release(ref byte[] buffer)
    {
        if (Interlocked.Exchange(ref buffer, null!) is { } b)
        {
            var bufferHolder = this.bufferHolders[b.Length % BufferHolders];
            bufferHolder.Release(b);
        }
    }
}
