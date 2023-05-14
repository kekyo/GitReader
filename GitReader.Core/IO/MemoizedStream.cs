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
using GitReader.Internal;

namespace GitReader.IO;

internal sealed class MemoizedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private const int memoizeToFileSize = 1024 * 1024;

    private readonly BufferPoolBuffer temporaryBuffer = BufferPool.Take(memoizeToFileSize);

    private Stream parent;
    private Stream memoized;
    private TemporaryFile? temporaryFile;

    internal MemoizedStream(
        Stream parent, long parentLength, TemporaryFile? temporaryFile, Stream memoized)
    {
        this.parent = parent;
        Length = parentLength;
        this.temporaryFile = temporaryFile;
        this.memoized = memoized;
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
        get => memoized.Position;
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

        temporaryBuffer.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    private long PrepareSeek(
        long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => memoized.Position + offset,
            _ => Length - offset,
        };

        if (position > Length)
        {
            position = Length;
        }

        return position;
    }

    public override long Seek(
        long offset, SeekOrigin origin)
    {
        var position = PrepareSeek(offset, origin);

        if (position < memoized.Length)
        {
            memoized.Seek(
                position, SeekOrigin.Begin);
            return position;
        }

        memoized.Seek(
            memoized.Length, SeekOrigin.Begin);

        var current = memoized.Length;
        while (current < position)
        {
            var length = Math.Min(
                position - current,
                temporaryBuffer.Length);

            var read = parent.Read(
                temporaryBuffer, 0, (int)length);
            if (read == 0)
            {
                break;
            }

            memoized.Write(
                temporaryBuffer, 0, read);

            current += read;
        }

        return current;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct)
    {
        var position = PrepareSeek(offset, origin);

        if (position < memoized.Length)
        {
            memoized.Seek(
                position, SeekOrigin.Begin);
            return position;
        }

        memoized.Seek(
            memoized.Length, SeekOrigin.Begin);

        var current = memoized.Length;
        while (current < position)
        {
            var length = Math.Min(
                position - current,
                temporaryBuffer.Length);

            var read = parent is IValueTaskStream vts ?
                await vts.ReadValueTaskAsync(
                    temporaryBuffer, 0, (int)length, ct) :
                await parent.ReadAsync(
                    temporaryBuffer, 0, (int)length, ct);
            if (read == 0)
            {
                break;
            }

            if (memoized is MemoryStream)
            {
                memoized.Write(
                    temporaryBuffer, 0, read);
            }
            else
            {
                await memoized.WriteAsync(
                    temporaryBuffer, 0, read, ct);
            }

            current += read;
        }

        return current;
    }

    public Task<long> SeekAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        SeekValueTaskAsync(offset, origin, ct).AsTask();
#endif

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        if (count >= 1)
        {
            if (memoized.Position < memoized.Length)
            {
                read = memoized.Read(buffer, offset, count);

                offset += read;
                count -= read;
            }

            if (count >= 1)
            {
                if (memoized.Position >= memoized.Length)
                {
                    var length = Math.Min(
                        count, Length - memoized.Position);
                    if (length >= 1)
                    {
                        var r = parent.Read(buffer, offset, (int)length);
                        if (r >= 1)
                        {
                            memoized.Write(buffer, offset, r);
                            read += r;
                        }
                    }
                }
            }
        }

        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;

        if (count >= 1)
        {
            if (memoized.Position < memoized.Length)
            {
                read = memoized is MemoryStream ?
                    memoized.Read(buffer, offset, count) :
                    await memoized.ReadAsync(buffer, offset, count, ct);

                offset += read;
                count -= read;
            }

            if (count >= 1)
            {
                if (memoized.Position >= memoized.Length)
                {
                    var length = Math.Min(
                        count, Length - memoized.Position);
                    if (length >= 1)
                    {
                        var r = parent is IValueTaskStream vts ?
                            await vts.ReadValueTaskAsync(buffer, offset, (int)length, ct) :
                            await parent.ReadAsync(buffer, offset, (int)length, ct);

                        if (r >= 1)
                        {
                            if (memoized is MemoryStream)
                            {
                                memoized.Write(buffer, offset, r);
                            }
                            else
                            {
                                await memoized.WriteAsync(buffer, offset, r, ct);
                            }
                            read += r;
                        }
                    }
                }
            }
        }

        return read;
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<MemoizedStream> CreateAsync(
        Stream parent, long parentLength, CancellationToken ct)
#else
    public static async Task<MemoizedStream> CreateAsync(
        Stream parent, long parentLength, CancellationToken ct)
#endif
    {
        if (parentLength >= memoizeToFileSize)
        {
            var temporaryFile = TemporaryFile.CreateFile();
            return new(parent, parentLength, temporaryFile, temporaryFile.Stream);
        }
        else if (parentLength < 0)
        {
            var ms = new MemoryStream();
            await parent.CopyToAsync(ms, 65536, ct);
            ms.Position = 0;
            return new(ms, ms.Length, null, ms);
        }
        else
        {
            return new(parent, parentLength, null, new MemoryStream((int)parentLength));
        }
    }
}
