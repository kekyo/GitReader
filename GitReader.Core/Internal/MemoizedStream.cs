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

internal sealed class MemoizedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private const int memoizeToFileSize = 1024 * 1024;

    private readonly byte[] temporaryBuffer = new byte[memoizeToFileSize];

    private Stream parent;
    private Stream memoized;
    private TemporaryFile? temporaryFile;

    internal MemoizedStream(
        Stream parent, long parentLength, TemporaryFile? temporaryFile, Stream memoized)
    {
        this.parent = parent;
        this.Length = parentLength;
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
        get => this.memoized.Position;
        set => this.Seek(value, SeekOrigin.Begin);
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Close();
        }
    }

    private long PrepareSeek(
        long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.memoized.Position + offset,
            _ => this.Length - offset,
        };

        if (position > this.Length)
        {
            position = this.Length;
        }

        return position;
    }

    public override long Seek(
        long offset, SeekOrigin origin)
    {
        var position = this.PrepareSeek(offset, origin);

        if (position < this.memoized.Length)
        {
            this.memoized.Seek(
                position, SeekOrigin.Begin);
            return position;
        }

        this.memoized.Seek(
            this.memoized.Length, SeekOrigin.Begin);

        var current = this.memoized.Length;
        while (current < position)
        {
            var length = Math.Min(
                position - current,
                temporaryBuffer.Length);

            var read = this.parent.Read(
                temporaryBuffer, 0, (int)length);
            if (read == 0)
            {
                break;
            }

            this.memoized.Write(
                temporaryBuffer, 0, read);

            current += read;
        }

        return current;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct)
    {
        var position = this.PrepareSeek(offset, origin);

        if (position < this.memoized.Length)
        {
            this.memoized.Seek(
                position, SeekOrigin.Begin);
            return position;
        }

        this.memoized.Seek(
            this.memoized.Length, SeekOrigin.Begin);

        var current = this.memoized.Length;
        while (current < position)
        {
            var length = Math.Min(
                position - current,
                temporaryBuffer.Length);

            var read = this.parent is IValueTaskStream vts ?
                await vts.ReadValueTaskAsync(
                    temporaryBuffer, 0, (int)length, ct) :
                await this.parent.ReadAsync(
                    temporaryBuffer, 0, (int)length, ct);
            if (read == 0)
            {
                break;
            }

            if (this.memoized is MemoryStream)
            {
                this.memoized.Write(
                    temporaryBuffer, 0, read);
            }
            else
            {
                await this.memoized.WriteAsync(
                    temporaryBuffer, 0, read, ct);
            }

            current += read;
        }

        return current;
    }
#endif

    public async Task<long> SeekAsync(
        long offset, SeekOrigin origin, CancellationToken ct)
    {
        var position = this.PrepareSeek(offset, origin);

        if (position < this.memoized.Length)
        {
            this.memoized.Seek(
                position, SeekOrigin.Begin);
            return position;
        }

        this.memoized.Seek(
            this.memoized.Length, SeekOrigin.Begin);

        var current = this.memoized.Length;
        while (current < position)
        {
            var length = Math.Min(
                position - current,
                temporaryBuffer.Length);

            var read = await this.parent.ReadAsync(
                temporaryBuffer, 0, (int)length, ct);
            if (read == 0)
            {
                break;
            }

            if (this.memoized is MemoryStream)
            {
                this.memoized.Write(
                    temporaryBuffer, 0, read);
            }
            else
            {
                await this.memoized.WriteAsync(
                    temporaryBuffer, 0, read, ct);
            }

            current += read;
        }

        return current;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        if (count >= 1)
        {
            if (this.memoized.Position < this.memoized.Length)
            {
                read = this.memoized.Read(buffer, offset, count);

                offset += read;
                count -= read;
            }

            if (count >= 1)
            {
                if (this.memoized.Position >= this.memoized.Length)
                {
                    var length = Math.Min(
                        count, this.Length - this.memoized.Position);
                    if (length >= 1)
                    {
                        var r = this.parent.Read(buffer, offset, (int)length);
                        if (r >= 1)
                        {
                            this.memoized.Write(buffer, offset, r);
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
            if (this.memoized.Position < this.memoized.Length)
            {
                if (this.memoized is MemoryStream)
                {
                    read = this.memoized.Read(buffer, offset, count);
                }
                else
                {
                    read = await this.memoized.ReadAsync(buffer, offset, count, ct);
                }

                offset += read;
                count -= read;
            }

            if (count >= 1)
            {
                if (this.memoized.Position >= this.memoized.Length)
                {
                    var length = Math.Min(
                        count, this.Length - this.memoized.Position);
                    if (length >= 1)
                    {
                        int r;
                        if (this.parent is IValueTaskStream vts)
                        {
                            r = await vts.ReadValueTaskAsync(buffer, offset, (int)length, ct);
                        }
                        else
                        {
                            r = await this.parent.ReadAsync(buffer, offset, (int)length, ct);
                        }

                        if (r >= 1)
                        {
                            if (this.memoized is MemoryStream)
                            {
                                this.memoized.Write(buffer, offset, r);
                            }
                            else
                            {
                                await this.memoized.WriteAsync(buffer, offset, r, ct);
                            }
                            read += r;
                        }
                    }
                }
            }
        }

        return read;
    }
#endif

#if !NET35 && !NET40
    private async Task<int> InternalReadAsync2(
        byte[] buffer, int offset, int count, int read, CancellationToken ct)
    {
        var length = Math.Min(
            count, this.Length - this.memoized.Position);
        if (length >= 1)
        {
            var r = await this.parent.ReadAsync(buffer, offset, (int)length, ct);
            if (r >= 1)
            {
                if (this.memoized is MemoryStream)
                {
                    this.memoized.Write(buffer, offset, r);
                }
                else
                {
                    await this.memoized.WriteAsync(buffer, offset, r, ct);
                }
                read += r;
            }
        }

        return read;
    }

    private async Task<int> InternalReadAsync1(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = await this.memoized.ReadAsync(buffer, offset, count, ct);

        offset += read;
        count -= read;

        if (count >= 1)
        {
            if (this.memoized.Position >= this.memoized.Length)
            {
                var length = Math.Min(
                    count, this.Length - this.memoized.Position);
                if (length >= 1)
                {
                    var r = await this.parent.ReadAsync(buffer, offset, (int)length, ct);

                    if (r >= 1)
                    {
                        await this.memoized.WriteAsync(buffer, offset, r, ct);
                        read += r;
                    }
                }
            }
        }

        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;

        if (count >= 1)
        {
            if (this.memoized.Position < this.memoized.Length)
            {
                if (this.memoized is MemoryStream)
                {
                    read = this.memoized.Read(buffer, offset, count);
                }
                else
                {
                    return this.InternalReadAsync1(buffer, offset, count, ct);
                }

                offset += read;
                count -= read;
            }

            if (count >= 1)
            {
                if (this.memoized.Position >= this.memoized.Length)
                {
                    return this.InternalReadAsync2(buffer, offset, count, read, ct);
                }
            }
        }

        return Utilities.FromResult(read);
    }
#endif

    public override void Flush() =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();

    public static MemoizedStream Create(Stream parent, long parentLength)
    {
        if (parentLength >= memoizeToFileSize)
        {
            var temporaryFile = TemporaryFile.CreateFile();
            return new(parent, parentLength, temporaryFile, temporaryFile.Stream);
        }
        else
        {
            return new(parent, parentLength, null, new MemoryStream((int)parentLength));
        }
    }
}
