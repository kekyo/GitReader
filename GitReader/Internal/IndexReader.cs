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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class IndexReader
{
    public static async Task<Dictionary<Hash, ulong>> ReadIndexAsync(
        string indexPath, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new FormatException($"Broken index file: File={indexPath}, Step={step}");

        using var fs = new FileStream(
            indexPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);

        var header = new byte[8];
        var read = await fs.ReadAsync(header, 0, header.Length).
            WaitAsync(ct);
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
        read = await fs.ReadAsync(fanoutTableBuffer, 0, fanoutTableBuffer.Length).
            WaitAsync(ct);
        if (read != fanoutTableBuffer.Length)
        {
            Throw(4);
        }

        Utilities.MakeBigEndian(fanoutTableBuffer, 255 * 4, 4);
        var objectCount = BitConverter.ToUInt32(fanoutTableBuffer, 255 * 4);
        if (objectCount == 0)
        {
            return new();
        }

        // Read hash table.
        var sha1List = new Hash[objectCount];
        var hashTableBuffer = new byte[8192 * 20];

        for (var index = 0; index < objectCount; )
        {
            var request = Math.Min((objectCount - index) * 20, hashTableBuffer.Length);
            read = await fs.ReadAsync(hashTableBuffer, 0, (int)request).
                WaitAsync(ct);
            if (read != request)
            {
                Throw(5);
            }

            for (var i = 0; i < read; i += 20)
            {
                if (index >= sha1List.Length)
                {
                    Throw(6);
                }

                var buffer = new byte[20];
                Array.Copy(hashTableBuffer, i, buffer, 0, buffer.Length);

                sha1List[index] = buffer;
                index++;
            }
        }

        // Skip crc32 table.
        var offsetTableBuffer = new byte[objectCount * 4];
        read = await fs.ReadAsync(offsetTableBuffer, 0, offsetTableBuffer.Length).  // dummy read
            WaitAsync(ct);
        if (read != offsetTableBuffer.Length)
        {
            Throw(7);
        }

        // Read offset table.
        read = await fs.ReadAsync(offsetTableBuffer, 0, offsetTableBuffer.Length).
            WaitAsync(ct);
        if (read != offsetTableBuffer.Length)
        {
            Throw(8);
        }

        var dict = new Dictionary<Hash, ulong>((int)objectCount);
        var largeOffsetDict = new Dictionary<Hash, uint>();

        for (var index = 0; index < objectCount; index++)
        {
            Utilities.MakeBigEndian(offsetTableBuffer, index * 4, 4);
            var offset = BitConverter.ToUInt32(offsetTableBuffer, index * 4);
            if (offset >= 0x80000000U)
            {
                offset &= 0x7fffffffU;
                largeOffsetDict.Add(sha1List[index], offset);
            }
            else
            {
                dict[sha1List[index]] = offset;
            }
        }

        if (largeOffsetDict.Count >= 1)
        {
            // Read large offset table.
            var largeOffsetTableBuffer = new byte[largeOffsetDict.Count * 8];
            read = await fs.ReadAsync(largeOffsetTableBuffer, 0, largeOffsetTableBuffer.Length).
                WaitAsync(ct);
            if (read != largeOffsetTableBuffer.Length)
            {
                Throw(9);
            }

            for (var index = 0; index < largeOffsetTableBuffer.Length; index++)
            {
                Utilities.MakeBigEndian(largeOffsetTableBuffer, index * 8, 8);
            }

            foreach (var entry in largeOffsetDict)
            {
                if (entry.Value >= largeOffsetDict.Count)
                {
                    Throw(10);
                }

                var offset = BitConverter.ToUInt64(offsetTableBuffer, (int)entry.Value * 8);
                dict[entry.Key] = offset;
            }
        }

        return dict;
    }
}
