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

namespace GitReader.IO;

internal sealed class ConcatStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
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
        for (var index = 0; index < streams.Length; index++)
        {
            if (Interlocked.Exchange(ref streams[index], null!) is { } stream)
            {
                stream.Dispose();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;
        while (count >= 1 && streamIndex < streams.Length)
        {
            var stream = streams[streamIndex];

            var r = stream.Read(buffer, offset, count);

            if (r >= 1)
            {
                read += r;
                offset += r;
                count -= r;
            }
            else
            {
                Interlocked.Exchange(ref streams[streamIndex], null!).Dispose();
                streamIndex++;
            }
        }
        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;
        while (count >= 1 && streamIndex < streams.Length)
        {
            var stream = streams[streamIndex];

            var r = stream is IValueTaskStream vts ?
                await vts.ReadValueTaskAsync(buffer, offset, count, ct) :
                await stream.ReadAsync(buffer, offset, count, ct);

            if (r >= 1)
            {
                read += r;
                offset += r;
                count -= r;
            }
            else
            {
                Interlocked.Exchange(ref streams[streamIndex], null!).Dispose();
                streamIndex++;
            }
        }
        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
#endif

    public override long Length =>
        throw new NotImplementedException();

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotImplementedException();

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        throw new NotImplementedException();
#endif

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();

    public override void Flush() =>
        throw new NotImplementedException();
}
