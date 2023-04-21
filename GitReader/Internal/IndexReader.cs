////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal readonly struct ObjectEntry
{
    public readonly ulong Offset;
    public readonly uint Crc32;

    public ObjectEntry(ulong offset, uint crc32)
    {
        this.Offset = offset;
        this.Crc32 = crc32;
    }
}

internal static class IndexReader
{
    private const int hashTableBufferCount = 8192;

    public static async Task<Dictionary<Hash, ObjectEntry>> ReadIndexAsync(
        string indexPath, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Broken index file: File={indexPath}, Step={step}");

        using var fs = new FileStream(
            indexPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);

        var header = new byte[8];
        var read = await fs.ReadAsync(header, 0, header.Length, ct);
        if (read != header.Length)
        {
            Throw(1);
        }

        if (!(header[0] == 0xff &&   // signature
            header[1] == 0x74 &&
            header[2] == 0x4f &&
            header[3] == 0x63))
        {
            Throw(2);
        }

        Utilities.MakeBigEndian(header, 4, 4);
        var version = BitConverter.ToUInt32(header, 4);
        if (version != 2)
        {
            Throw(3);
        }

        // Skip fanout table.
        var fanoutTableBuffer = new byte[256 * 4];
        read = await fs.ReadAsync(fanoutTableBuffer, 0, fanoutTableBuffer.Length, ct);
        if (read != fanoutTableBuffer.Length)
        {
            Throw(4);
        }

        // Take object count from last fanout entry.
        Utilities.MakeBigEndian(fanoutTableBuffer, 255 * 4, 4);
        var objectCount = BitConverter.ToUInt32(fanoutTableBuffer, 255 * 4);
        if (objectCount == 0)
        {
            return new();
        }

        // Read hash table.
        var sha1List = new Hash[objectCount];
        var hashTableBuffer = new byte[hashTableBufferCount * Hash.Size];

        for (var index = 0; index < objectCount; )
        {
            var request = (int)Math.Min((objectCount - index) * Hash.Size, hashTableBuffer.Length);
            read = await fs.ReadAsync(hashTableBuffer, 0, request, ct);
            if (read != request)
            {
                Throw(5);
            }

            for (var i = 0; i < read; i += Hash.Size)
            {
                var buffer = new byte[Hash.Size];
                Array.Copy(hashTableBuffer, i, buffer, 0, buffer.Length);

                sha1List[index] = buffer;
                index++;
            }
        }

        // Read CRC32 table.
        var crc32TableBuffer = new byte[objectCount * 4];
        read = await fs.ReadAsync(crc32TableBuffer, 0, crc32TableBuffer.Length, ct);
        if (read != crc32TableBuffer.Length)
        {
            Throw(7);
        }

        // Read offset table.
        var offsetTableBuffer = new byte[objectCount * 4];
        read = await fs.ReadAsync(offsetTableBuffer, 0, offsetTableBuffer.Length, ct);
        if (read != offsetTableBuffer.Length)
        {
            Throw(8);
        }

        var dict = new Dictionary<Hash, ObjectEntry>((int)objectCount);
        var largeOffsetOffset = new List<KeyValuePair<int, uint>>();

        for (var index = 0; index < objectCount; index++)
        {
            Utilities.MakeBigEndian(offsetTableBuffer, index * 4, 4);
            var offset = BitConverter.ToUInt32(offsetTableBuffer, index * 4);
            if (offset >= 0x80000000U)
            {
                offset &= 0x7fffffffU;
                largeOffsetOffset.Add(new(index, offset));
            }
            else
            {
                Utilities.MakeBigEndian(crc32TableBuffer, index * 4, 4);
                var crc32 = BitConverter.ToUInt32(crc32TableBuffer, index * 4);
                dict[sha1List[index]] = new(offset, crc32);
            }
        }

        // Read large offset table.
        if (largeOffsetOffset.Count >= 1)
        {
            var largeOffsetTableBuffer = new byte[largeOffsetOffset.Count * 8];
            read = await fs.ReadAsync(largeOffsetTableBuffer, 0, largeOffsetTableBuffer.Length, ct);
            if (read != largeOffsetTableBuffer.Length)
            {
                Throw(9);
            }

            for (var index = 0; index < largeOffsetTableBuffer.Length; index++)
            {
                Utilities.MakeBigEndian(largeOffsetTableBuffer, index * 8, 8);
            }

            foreach (var entry in largeOffsetOffset)
            {
                if (entry.Value >= largeOffsetOffset.Count)
                {
                    Throw(10);
                }

                var offset = BitConverter.ToUInt64(offsetTableBuffer, (int)entry.Value * 8);

                Utilities.MakeBigEndian(crc32TableBuffer, entry.Key * 4, 4);
                var crc32 = BitConverter.ToUInt32(crc32TableBuffer, entry.Key * 4);

                dict[sha1List[entry.Key]] = new(offset, crc32);
            }
        }

        return dict;
    }
}
