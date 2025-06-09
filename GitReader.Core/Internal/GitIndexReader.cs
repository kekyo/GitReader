////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class GitIndexReader
{
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<GitIndexEntry[]> ReadIndexEntriesAsync(
        Repository repository, CancellationToken ct)
#else
    public static async Task<GitIndexEntry[]> ReadIndexEntriesAsync(
        Repository repository, CancellationToken ct)
#endif
    {
        var indexPath = repository.fileSystem.Combine(repository.GitPath, "index");
        if (!await repository.fileSystem.IsFileExistsAsync(indexPath, ct))
        {
            return new GitIndexEntry[0];
        }

        void Throw(int step) =>
            throw new InvalidDataException(
                $"Broken Git index file: File={indexPath}, Step={step}");

        using var fs = await repository.fileSystem.OpenAsync(indexPath, false, ct);
        using var header = repository.pool.Take(12);

        // Read index header (12 bytes)
        var read = await fs.ReadAsync(header, 0, header.Length, ct);
        if (read != header.Length)
        {
            Throw(1);
        }

        // Verify signature "DIRC"
        if (!(header[0] == 0x44 &&   // 'D'
              header[1] == 0x49 &&   // 'I'
              header[2] == 0x52 &&   // 'R'
              header[3] == 0x43))    // 'C'
        {
            Throw(2);
        }

        // Read version (big-endian)
        Utilities.MakeBigEndian(header, 4, 4);
        var version = BitConverter.ToUInt32(header, 4);
        if (version < 2 || version > 4)
        {
            Throw(3);
        }

        // Read entry count (big-endian)
        Utilities.MakeBigEndian(header, 8, 4);
        var entryCount = BitConverter.ToUInt32(header, 8);
        if (entryCount > int.MaxValue)
        {
            Throw(4);
        }

        var entries = new List<GitIndexEntry>();
        using var entryBuffer = repository.pool.Take(62); // Minimum entry size

        for (var i = 0; i < entryCount; i++)
        {
            // Read fixed part of entry (62 bytes)
            read = await fs.ReadAsync(entryBuffer, 0, entryBuffer.Length, ct);
            if (read != entryBuffer.Length)
            {
                Throw(5);
            }

            // Parse fixed fields (all big-endian)
            Utilities.MakeBigEndian(entryBuffer, 0, 4);
            var creationTime = BitConverter.ToUInt32(entryBuffer, 0);
            
            Utilities.MakeBigEndian(entryBuffer, 4, 4);
            var creationTimeNano = BitConverter.ToUInt32(entryBuffer, 4);
            
            Utilities.MakeBigEndian(entryBuffer, 8, 4);
            var modificationTime = BitConverter.ToUInt32(entryBuffer, 8);
            
            Utilities.MakeBigEndian(entryBuffer, 12, 4);
            var modificationTimeNano = BitConverter.ToUInt32(entryBuffer, 12);
            
            Utilities.MakeBigEndian(entryBuffer, 16, 4);
            var device = BitConverter.ToUInt32(entryBuffer, 16);
            
            Utilities.MakeBigEndian(entryBuffer, 20, 4);
            var inode = BitConverter.ToUInt32(entryBuffer, 20);
            
            Utilities.MakeBigEndian(entryBuffer, 24, 4);
            var mode = BitConverter.ToUInt32(entryBuffer, 24);
            
            Utilities.MakeBigEndian(entryBuffer, 28, 4);
            var userId = BitConverter.ToUInt32(entryBuffer, 28);
            
            Utilities.MakeBigEndian(entryBuffer, 32, 4);
            var groupId = BitConverter.ToUInt32(entryBuffer, 32);
            
            Utilities.MakeBigEndian(entryBuffer, 36, 4);
            var fileSize = BitConverter.ToUInt32(entryBuffer, 36);

            // Object hash (20 bytes)
            var objectHash = new Hash(entryBuffer, 40);

            // Flags (2 bytes, big-endian)
            Utilities.MakeBigEndian(entryBuffer, 60, 2);
            var flags = BitConverter.ToUInt16(entryBuffer, 60);

            // Path length from flags
            var pathLength = flags & 0x0FFF;
            
            // Read path name
            string path;
            if (pathLength < 0x0FFF)
            {
                // Normal path length
                using var pathBuffer = repository.pool.Take((int)pathLength);
                read = await fs.ReadAsync(pathBuffer, 0, (int)pathLength, ct);
                if (read != pathLength)
                {
                    Throw(6);
                }
                path = Encoding.UTF8.GetString(pathBuffer, 0, (int)pathLength);
            }
            else
            {
                // Extended path (read until null terminator)
                var pathBytes = new List<byte>();
                using var singleByte = repository.pool.Take(1);
                while (true)
                {
                    read = await fs.ReadAsync(singleByte, 0, 1, ct);
                    if (read != 1)
                    {
                        Throw(7);
                    }
                    if (singleByte[0] == 0)
                    {
                        break;
                    }
                    pathBytes.Add(singleByte[0]);
                }
                path = Encoding.UTF8.GetString(pathBytes.ToArray());
            }

            // Skip padding to align to 8-byte boundary
            var totalEntryLength = 62 + (pathLength < 0x0FFF ? pathLength : path.Length + 1);
            var padding = (8 - (totalEntryLength % 8)) % 8;
            if (padding > 0)
            {
                using var paddingBuffer = repository.pool.Take((int)padding);
                read = await fs.ReadAsync(paddingBuffer, 0, (int)padding, ct);
                if (read != padding)
                {
                    Throw(8);
                }
            }

            entries.Add(new GitIndexEntry(
                creationTime,
                creationTimeNano,
                modificationTime,
                modificationTimeNano,
                device,
                inode,
                mode,
                userId,
                groupId,
                fileSize,
                objectHash,
                flags,
                path));
        }

        return entries.ToArray();
    }
} 