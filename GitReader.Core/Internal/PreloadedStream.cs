﻿////////////////////////////////////////////////////////////////////////////
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

internal sealed class PreloadedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private readonly byte[] preloadedBuffer;
    private readonly int preloadedLength;
    private int preloadedIndex;

    public PreloadedStream(byte[] buffer, int initialIndex, int length)
    {
        this.preloadedBuffer = buffer;
        this.preloadedLength = length;
        this.preloadedIndex = initialIndex;
        this.Length = length - initialIndex;
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

    public override long Length { get; }

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
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
        var length = Math.Min(count, this.preloadedLength - this.preloadedIndex);
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
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var length = Math.Min(count, this.preloadedLength - this.preloadedIndex);
        if (length >= 1)
        {
            Array.Copy(this.preloadedBuffer, this.preloadedIndex, buffer, offset, length);
            this.preloadedIndex += length;
            return new(length);
        }
        else
        {
            return new(0);
        }
    }
#endif

#if !NET35 && !NET40
    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var length = Math.Min(count, this.preloadedLength - this.preloadedIndex);
        if (length >= 1)
        {
            Array.Copy(this.preloadedBuffer, this.preloadedIndex, buffer, offset, length);
            this.preloadedIndex += length;
            return Utilities.FromResult(length);
        }
        else
        {
            return Utilities.FromResult(0);
        }
    }
#endif

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