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

internal sealed class ConcatStream : Stream
{
    private readonly Stream[] streams;
    private int streamIndex;

    public ConcatStream(params Stream[] streams) =>
        this.streams = streams;

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
        for (var index = 0; index < this.streams.Length; index++)
        {
            if (Interlocked.Exchange(ref this.streams[index], null!) is { } stream)
            {
                stream.Dispose();
            }
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
        var read = 0;
        while (count >= 1 && this.streamIndex < this.streams.Length)
        {
            var stream = this.streams[this.streamIndex];
            var r = stream.Read(buffer, offset, count);
            if (r >= 1)
            {
                read += r;
                offset += r;
                count -= r;
            }
            else
            {
                Interlocked.Exchange(ref this.streams[this.streamIndex], null!).Dispose();
                this.streamIndex++;
            }
        }
        return read;
    }

#if !NET35 && !NET40
    public override async Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;
        while (count >= 1 && this.streamIndex < this.streams.Length)
        {
            var stream = this.streams[this.streamIndex];
            var r = await stream.ReadAsync(buffer, offset, count, ct);
            if (r >= 1)
            {
                read += r;
                offset += r;
                count -= r;
            }
            else
            {
                Interlocked.Exchange(ref this.streams[this.streamIndex], null!).Dispose();
                this.streamIndex++;
            }
        }
        return read;
    }
#endif

    public override long Length =>
        throw new NotImplementedException();

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override void Flush() =>
        throw new NotImplementedException();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();
}
