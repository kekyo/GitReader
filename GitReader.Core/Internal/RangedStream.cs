////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal sealed class RangedStream : Stream
{
    private Stream parent;
    private long remains;

    public RangedStream(Stream parent, long length)
    {
        this.parent = parent;
        this.remains = length;
        this.Length = length;
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

    public override long Length { get; }

    public override long Position
    {
        get => this.Length - this.remains;
        set => throw new NotImplementedException();
    }

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
        if (Interlocked.Exchange(ref this.parent, null!) is { } parent)
        {
            parent.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Close();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remains = (int)Math.Min(count, this.remains);
        if (remains == 0)
        {
            return 0;
        }

        var read = this.parent.Read(buffer, offset, remains);

        this.remains -= read;
        return read;
    }

#if !NET35 && !NET40
    private async Task<int> InternalReadAsync(
        byte[] buffer, int offset, int remains, CancellationToken ct)
    {
        var read = await this.parent.ReadAsync(buffer, offset, remains, ct);

        this.remains -= read;
        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var remains = (int)Math.Min(count, this.remains);
        if (remains >= 1)
        {
            return this.InternalReadAsync(buffer, offset, remains, ct);
        }
        else
        {
            return Utilities.FromResult(0);
        }
    }
#endif

    public override void Flush() =>
        throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();
}
