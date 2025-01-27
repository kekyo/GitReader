////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

internal sealed class PreloadedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private readonly BufferPoolBuffer preloadedBuffer;
    private readonly int preloadedLength;
    private int preloadedIndex;

    public PreloadedStream(
        DetachedBufferPoolBuffer buffer, int initialIndex, int totalLength)
    {
        this.preloadedBuffer = buffer;
        this.preloadedLength = totalLength;
        this.preloadedIndex = initialIndex;
    }

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
        this.preloadedBuffer.Dispose();
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
        var length = Math.Min(
            count, this.preloadedLength - this.preloadedIndex);
        if (length >= 1)
        {
            Array.Copy(this.preloadedBuffer, this.preloadedIndex, buffer, offset, length);
            this.preloadedIndex += length;
            return length;
        }
        else
        {
            return 0;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        new(this.Read(buffer, offset, count));

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        this.ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
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
