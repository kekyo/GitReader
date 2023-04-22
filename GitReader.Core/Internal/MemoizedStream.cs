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
{
    private static readonly byte[] dummyBuffer = new byte[16384];

    private Stream parent;
    private Stream memoized;
    private TemporaryFile? temporaryFile;

    public MemoizedStream(Stream parent, bool useTemporaryFile)
    {
        this.parent = parent;
        if (useTemporaryFile)
        {
            this.temporaryFile = TemporaryFile.CreateFile();
            this.memoized = this.temporaryFile.Stream;
        }
        else
        {
            this.memoized = new MemoryStream();
        }
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        true;
    public override bool CanWrite =>
        false;

    public override long Length =>
        this.memoized.Length;

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

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.memoized.Position + offset,
            _ => throw new InvalidOperationException(),
        };

        if (position > this.memoized.Length)
        {
            do
            {
                var count = Math.Min(position - this.memoized.Length, dummyBuffer.Length);
                var read = this.Read(dummyBuffer, 0, (int)count);
                if (read == 0)
                {
                    break;
                }
            }
            while (position > this.memoized.Length);

            Debug.Assert(position == this.memoized.Position);
        }
        else
        {
            this.memoized.Seek(position, SeekOrigin.Begin);
        }

        return this.memoized.Position;
    }

    public async Task<long> SeekAsync(
        long offset, SeekOrigin origin, CancellationToken ct)
    {
        var position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.memoized.Position + offset,
            _ => throw new InvalidOperationException(),
        };

        if (position > this.memoized.Length)
        {
            do
            {
                var count = Math.Min(position - this.memoized.Length, dummyBuffer.Length);
                var read = await this.ReadAsync(dummyBuffer, 0, (int)count, ct);
                if (read == 0)
                {
                    break;
                }
            }
            while (position > this.memoized.Length);

            Debug.Assert(position == this.memoized.Position);
        }
        else
        {
            this.memoized.Seek(position, SeekOrigin.Begin);
        }

        return this.memoized.Position;
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
                    var r = this.parent.Read(buffer, offset, count);
                    if (r >= 1)
                    {
                        this.memoized.Write(buffer, offset, r);
                        read += r;
                    }
                }
            }
        }

        return read;
    }

#if !NET35 && !NET40
    public override async Task<int> ReadAsync(
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
                    var r = await this.parent.ReadAsync(buffer, offset, count, ct);
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

        return read;
    }
#endif

    public override void Flush() =>
        throw new NotImplementedException();

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();
}
