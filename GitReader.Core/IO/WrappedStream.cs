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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.IO;

internal sealed class WrappedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private sealed class ParentStreamContext
    {
        public readonly AsyncLock Locker = new();
        public readonly Stream Parent;
        public int Count = 1;

        public ParentStreamContext(Stream parent) =>
            Parent = parent;
    }

    private ParentStreamContext context;
    private long position;

    public WrappedStream(Stream parent)
    {
        Debug.Assert(parent.CanSeek);
        context = new(parent);
    }

    private WrappedStream(ParentStreamContext context)
    {
        this.context = context;
        Interlocked.Increment(ref this.context.Count);
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

    public override long Length =>
        context.Parent.Length;

    public override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
        if (Interlocked.Exchange(ref this.context, null!) is { } context &&
            Interlocked.Decrement(ref context.Count) is { } count &&
            count == 0)
        {
            context.Parent.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    internal WrappedStream Clone() =>
        new(context);

    public override long Seek(
        long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                position = offset;
                break;
            case SeekOrigin.Current:
                position += offset;
                break;
            case SeekOrigin.End:
                position = Length;
                return position;
        }

        if (position > Length)
        {
            position = Length;
        }

        return position;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        new(Seek(offset, origin));
#endif

    public override int Read(
        byte[] buffer, int offset, int count)
    {
        using var _ = context.Locker.Lock();

        context.Parent.Seek(position, SeekOrigin.Begin);
        var read = context.Parent.Read(buffer, offset, count);
        position += read;
        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        using var _ = await context.Locker.LockAsync(ct);

        if (context.Parent is IValueTaskStream vts)
        {
            await vts.SeekValueTaskAsync(position, SeekOrigin.Begin, ct);
            var read = await vts.ReadValueTaskAsync(buffer, offset, count, ct);
            position += read;
            return read;
        }
        else
        {
            context.Parent.Seek(position, SeekOrigin.Begin);
            var read = await context.Parent.ReadAsync(buffer, offset, count, ct);
            position += read;
            return read;
        }
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
#endif

    public override void Flush() =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();
}
