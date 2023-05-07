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

namespace GitReader.Internal;

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
            this.Parent = parent;
    }

    private ParentStreamContext context;
    private long position;

    public WrappedStream(Stream parent)
    {
        Debug.Assert(parent.CanSeek);
        this.context = new(parent);
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
        this.context.Parent.Length;

    public override long Position
    {
        get => this.position;
        set => this.Seek(value, SeekOrigin.Begin);
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
            this.Close();
        }
    }

    internal WrappedStream Clone() =>
        new(this.context);

    public override long Seek(
        long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                this.position = offset;
                break;
            case SeekOrigin.Current:
                this.position += offset;
                break;
            case SeekOrigin.End:
                this.position = this.Length;
                return this.position;
        }

        if (this.position > this.Length)
        {
            this.position = this.Length;
        }

        return this.position;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        new(this.Seek(offset, origin));
#endif

    public override int Read(
        byte[] buffer, int offset, int count)
    {
        using var _ = this.context.Locker.Lock();

        this.context.Parent.Seek(this.position, SeekOrigin.Begin);
        var read = this.context.Parent.Read(buffer, offset, count);
        this.position += read;
        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        using var _ = await this.context.Locker.LockAsync(ct);

        if (this.context.Parent is IValueTaskStream vts)
        {
            await vts.SeekValueTaskAsync(this.position, SeekOrigin.Begin, ct);
            var read = await vts.ReadValueTaskAsync(buffer, offset, count, ct);
            this.position += read;
            return read;
        }
        else
        {
            this.context.Parent.Seek(this.position, SeekOrigin.Begin);
            var read = await this.context.Parent.ReadAsync(buffer, offset, count, ct);
            this.position += read;
            return read;
        }
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        this.ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
#endif

    public override void Flush() =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();
}
