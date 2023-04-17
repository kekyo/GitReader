////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal sealed class ObjectStream : Stream
{
    private readonly DeflateStream ds;
    private readonly byte[] preload;
    private int preloadIndex;

    private ObjectStream(
        DeflateStream ds, byte[] preload, int preloadIndex, string type, long length)
    {
        this.ds = ds;
        this.preload = preload;
        this.preloadIndex = preloadIndex;
        Type = type;
        Length = length;
    }

    public string Type { get; }
    public override long Length { get; }
    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var copied = 0;

        if (this.preloadIndex < this.preload.Length)
        {
            var length = Math.Min(count, this.preload.Length - this.preloadIndex);
            Array.Copy(this.preload, this.preloadIndex, buffer, offset, length);
            this.preloadIndex += length;
            offset += length;
            count -= length;
            copied += length;
        }

        if (count >= 1)
        {
            var read = this.ds.Read(buffer, offset, count);
            return copied + read;
        }
        else
        {
            return copied;
        }
    }

#if !NET35 && !NET40
    public override async Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var copied = 0;

        if (this.preloadIndex < this.preload.Length)
        {
            var length = Math.Min(count, this.preload.Length - this.preloadIndex);
            Array.Copy(this.preload, this.preloadIndex, buffer, offset, length);
            this.preloadIndex += length;
            offset += length;
            count -= length;
            copied += length;
        }

        if (count >= 1)
        {
            var read = await this.ds.ReadAsync(buffer, offset, count, ct);
            return copied + read;
        }
        else
        {
            return copied;
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

    public static async Task<ObjectStream> CreateAsync(
        Stream parent, Hash hash, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new FormatException(
                $"Could not parse the object. Hash={hash}, Step={step}");

        var buffer = new byte[2];
        var read = await parent.ReadAsync(buffer, 0, buffer.Length).
            WaitAsync(ct);

        if (read < 2)
        {
            Throw(1);
        }

        if (buffer[0] != 0x78)
        {
            Throw(2);
        }

        switch (buffer[1])
        {
            case 0x01:
            case 0x5e:
            case 0x9c:
            case 0xda:
                break;
            default:
                Throw(3);
                break;
        }

        var ds = new DeflateStream(
            parent, CompressionMode.Decompress, false);

        var preload = new byte[256];
        read = await ds.ReadAsync(preload, 0, preload.Length).
            WaitAsync(ct);

        var preloadIndex = 0;
        while (true)
        {
            if (preloadIndex >= read)
            {
                Throw(4);
            }

            if (preload[preloadIndex] == 0x00)
            {
                break;
            }

            preloadIndex++;
        }

        var headerElements = Encoding.UTF8.GetString(preload, 0, preloadIndex).
            Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (headerElements.Length != 2)
        {
            Throw(5);
        }

        var type = headerElements[0];
        if (!long.TryParse(
            headerElements[1],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var length))
        {
            Throw(6);
        }

        return new(ds, preload, preloadIndex + 1, type, length);
    }
}
