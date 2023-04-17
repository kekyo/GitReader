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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal sealed class PackedObjectStream : Stream
{
    private readonly DeflateStream ds;
    private readonly byte[] preload;
    private int preloadIndex;

    private PackedObjectStream(
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

    public static async Task<PackedObjectStream> CreateAsync(
        string basePath, Hash hash, CancellationToken ct)
    {
        var results = await Utilities.WhenAll(
            Utilities.EnumerateFiles(basePath, "pack-*.idx").
            Select(indexPath => IndexReader.ReadIndexAsync(indexPath, ct)));

        return null!;
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
