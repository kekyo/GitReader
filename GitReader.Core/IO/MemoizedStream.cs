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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

internal sealed class MemoizedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private const int memoizeToFileSize = 1024 * 1024;

    private readonly BufferPoolBuffer temporaryBuffer;

    private Stream parent;
    private Stream memoized;
    private TemporaryFile? temporaryFile;

    internal MemoizedStream(
        Stream parent, long parentLength,
        TemporaryFile? temporaryFile, Stream memoized,
        BufferPool pool)
    {
        this.parent = parent;
        this.Length = parentLength;
        this.temporaryFile = temporaryFile;
        this.memoized = memoized;
        this.temporaryBuffer = pool.Take(memoizeToFileSize);
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        true;
    public override bool CanWrite =>
        false;

    public override long Length { get; }

    public override long Position
    {
        get => this.memoized.Position;
        set => Seek(value, SeekOrigin.Begin);
    }

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
        if (Interlocked.Exchange(ref this.parent!, null) is { } parent)
        {
            parent.Dispose();
        }
        if (Interlocked.Exchange(ref this.memoized!, null) is { } memoized)
        {
            memoized.Dispose();
        }
        if (Interlocked.Exchange(ref this.temporaryFile!, null) is { } temporaryFile)
        {
            temporaryFile.Dispose();
        }

        this.temporaryBuffer.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    private long GetTargetPosition(
        long offset, SeekOrigin origin)
    {
        var targetPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.memoized.Position + offset,
            _ => this.Length - offset,
        };

        if (targetPosition > this.Length)
        {
            targetPosition = this.Length;
        }

        return targetPosition;
    }

    public override long Seek(
        long offset, SeekOrigin origin)
    {
        var targetPosition = this.GetTargetPosition(offset, origin);
        if (targetPosition < this.memoized.Length)
        {
            this.memoized.Seek(
                targetPosition, SeekOrigin.Begin);
            return targetPosition;
        }

        this.memoized.Seek(
            this.memoized.Length, SeekOrigin.Begin);

        var currentPosition = this.memoized.Length;
        while (currentPosition < targetPosition)
        {
            var length = Math.Min(
                targetPosition - currentPosition,
                this.temporaryBuffer.Length);

            var r = this.Read(
                this.temporaryBuffer, 0, (int)length);
            if (r == 0)
            {
                break;
            }

            currentPosition += r;
        }

        Debug.Assert(this.memoized.Position == currentPosition);
        Debug.Assert(this.memoized.Length == currentPosition);

        return currentPosition;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct)
    {
        var targetPosition = this.GetTargetPosition(offset, origin);
        if (targetPosition < this.memoized.Length)
        {
            this.memoized.Seek(
                targetPosition, SeekOrigin.Begin);
            return targetPosition;
        }

        this.memoized.Seek(
            this.memoized.Length, SeekOrigin.Begin);

        var currentPosition = this.memoized.Length;
        while (currentPosition < targetPosition)
        {
            var length = Math.Min(
                targetPosition - currentPosition,
                this.temporaryBuffer.Length);

            var r = await this.ReadValueTaskAsync(
                this.temporaryBuffer, 0, (int)length, ct);
            if (r == 0)
            {
                break;
            }

            currentPosition += r;
        }

        Debug.Assert(this.memoized.Position == currentPosition);
        Debug.Assert(this.memoized.Length == currentPosition);

        return currentPosition;
    }

    public Task<long> SeekAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        this.SeekValueTaskAsync(offset, origin, ct).AsTask();
#endif

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        while (true)
        {
            var length = Math.Min(
                count, this.memoized.Length - this.memoized.Position);
            if (length <= 0)
            {
                break;
            }

            var r = this.memoized.Read(buffer, offset, (int)length);
            Debug.Assert(r == length);

            offset += r;
            count -= r;
            read += r;
        }

        while (true)
        {
            var length = Math.Min(
                count, this.Length - this.memoized.Position);
            if (length <= 0)
            {
                break;
            }

            var r = this.parent.Read(buffer, offset, (int)length);
            if (r == 0)
            {
                break;
            }

            Debug.Assert(this.memoized.Position == this.memoized.Length);

            this.memoized.Write(buffer, offset, r);

            offset += r;
            count -= r;
            read += r;
        }

        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;

        while (true)
        {
            var length = Math.Min(
                count, this.memoized.Length - this.memoized.Position);
            if (length <= 0)
            {
                break;
            }

            var r = this.memoized is MemoryStream ?
                this.memoized.Read(buffer, offset, (int)length) :
                await this.memoized.ReadAsync(buffer, offset, (int)length, ct);
            Debug.Assert(r == length);

            offset += r;
            count -= r;
            read += r;
        }

        while (true)
        {
            var length = Math.Min(
                count, this.Length - this.memoized.Position);
            if (length <= 0)
            {
                break;
            }

            var r = this.parent is IValueTaskStream vts ?
                await vts.ReadValueTaskAsync(buffer, offset, (int)length, ct) :
                await this.parent.ReadAsync(buffer, offset, (int)length, ct);
            if (r == 0)
            {
                break;
            }

            Debug.Assert(this.memoized.Position == this.memoized.Length);

            if (this.memoized is MemoryStream)
            {
                this.memoized.Write(buffer, offset, r);
            }
            else
            {
                await this.memoized.WriteAsync(buffer, offset, r, ct);
            }

            offset += r;
            count -= r;
            read += r;
        }

        return read;
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<MemoizedStream> CreateAsync(
        Stream parent,
        long parentLength,
        BufferPool pool,
        IFileSystem fileSystem,
        CancellationToken ct)
#else
    public static async Task<MemoizedStream> CreateAsync(
        Stream parent,
        long parentLength,
        BufferPool pool,
        IFileSystem fileSystem,
        CancellationToken ct)
#endif
    {
        if (parentLength >= memoizeToFileSize)
        {
            var temporaryFile = await TemporaryFile.CreateFileAsync(fileSystem, ct);
            return new(parent, parentLength, temporaryFile, temporaryFile.Stream, pool);
        }
        else if (parentLength < 0)
        {
            var ms = new MemoryStream();
            await parent.CopyToAsync(ms, 65536, pool, ct);
            ms.Position = 0;
            return new(ms, ms.Length, null, ms, pool);
        }
        else
        {
            return new(parent, parentLength, null, new MemoryStream((int)parentLength), pool);
        }
    }
}
