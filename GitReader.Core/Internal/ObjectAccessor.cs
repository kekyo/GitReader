////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal readonly struct ObjectStreamResult
{
    public readonly Stream Stream;
    public readonly ObjectTypes Type;

    public ObjectStreamResult(
        Stream stream, ObjectTypes type)
    {
        this.Stream = stream;
        this.Type = type;
    }
}

internal sealed class ObjectAccessor : IDisposable
{
    private const int preloadBufferSize = 65536;

    private readonly FileAccessor fileAccessor;
    private readonly string objectsBasePath;
    private readonly string packedBasePath;
    private readonly AsyncLock locker = new();
    private readonly Dictionary<string, WeakReference> indexCache = new();

    public ObjectAccessor(
        FileAccessor fileAccessor, string repositoryPath)
    {
        this.fileAccessor = fileAccessor;
        this.objectsBasePath = Utilities.Combine(
            repositoryPath,
            "objects");
        this.packedBasePath = Utilities.Combine(
            this.objectsBasePath,
            "pack");
    }

    public void Dispose()
    {
        using var _ = this.locker.Lock();
        this.indexCache.Clear();
    }

    //////////////////////////////////////////////////////////////////////////

    private async Task<ObjectStreamResult> OpenFromObjectFileAsync(
        string objectPath, Hash hash, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. Hash={hash}, Step={step}");

        var fs = this.fileAccessor.Open(objectPath);

        try
        {
            var zlibStream = await Utilities.CreateZLibStreamAsync(fs, ct);

            var preloadBuffer = new byte[preloadBufferSize];
            var read = await zlibStream.ReadAsync(
                preloadBuffer, 0, preloadBuffer.Length, ct);

            var preloadIndex = 0;
            while (true)
            {
                if (preloadIndex >= read)
                {
                    Throw(1);
                }

                if (preloadBuffer[preloadIndex] == 0x00)
                {
                    break;
                }

                preloadIndex++;
            }

            var headerElements = Encoding.UTF8.GetString(preloadBuffer, 0, preloadIndex).
                Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (headerElements.Length != 2)
            {
                Throw(2);
            }

            var typeString = headerElements[0];
            if (!Utilities.TryParse<ObjectTypes>(typeString, true, out var type))
            {
                Throw(3);
            }

            if (!ulong.TryParse(
                headerElements[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var length))
            {
                Throw(4);
            }
            if (length > long.MaxValue)
            {
                // Couldn't seek over ceil of int64 on .NET
                Throw(5);
            }

            preloadIndex++;

            var stream = new RangedStream(
                new ConcatStream(
                    new PreloadedStream(preloadBuffer, preloadIndex, preloadBuffer.Length - preloadIndex),
                    zlibStream),
                (long)length);

            return new(stream, type);
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }

    //////////////////////////////////////////////////////////////////////////

    private sealed class IndexEntry
    {
        public readonly string BaseFileName;
        public readonly Dictionary<Hash, ObjectEntry> ObjectEntries;

        public IndexEntry(
            string baseFileName, Dictionary<Hash, ObjectEntry> entries)
        {
            this.BaseFileName = baseFileName;
            this.ObjectEntries = entries;
        }
    }

    private async Task<IndexEntry> GetOrCacheIndexEntryAsync(
        string indexFileRelativePath, CancellationToken ct)
    {
        using var _ = await this.locker.LockAsync(ct);

        if (this.indexCache.TryGetValue(indexFileRelativePath, out var wr) &&
            wr.Target is IndexEntry cachedEntry)
        {
            return cachedEntry;
        }

        var dict = await IndexReader.ReadIndexAsync(
            Utilities.Combine(this.packedBasePath, indexFileRelativePath), ct);
        var entry = new IndexEntry(
            Utilities.Combine(
                Utilities.GetDirectoryPath(indexFileRelativePath),
                Path.GetFileNameWithoutExtension(indexFileRelativePath)),
            dict);

        this.indexCache[indexFileRelativePath] = new(entry);

        return entry;
    }

    //////////////////////////////////////////////////////////////////////////

    // https://git-scm.com/docs/pack-format

    public static bool TryGetVariableSize(
        byte[] buffer, int bufferCount, ref int index, out ulong value, int initialBits = 7)
    {
        if (index >= bufferCount)
        {
            value = 0;
            return false;
        }

        var hb = buffer[index++];
        var mask = (byte)(~(0xff << initialBits));
        value = (uint)(hb & mask);

        var shift = initialBits;

        while ((hb & 0x80) != 0)
        {
            if (index >= bufferCount)
            {
                return false;
            }
            if (shift >= 64)
            {
                return false;
            }

            hb = buffer[index++];
            value |= ((ulong)(uint)(hb & 0x7f)) << shift;
            shift += 7;
        }

        return true;
    }

    public static bool TryGetVariableOffset(
        byte[] buffer, int bufferCount, ref int index, out ulong value)
    {
        value = ulong.MaxValue;
        var bits = 0;
        byte hb;
        do
        {
            value++;

            if (index >= bufferCount)
            {
                return false;
            }
            if (bits >= 64)
            {
                return false;
            }

            hb = buffer[index++];
            value = (value << 7) | (uint)(hb & 0x7f);
            bits += 7;
        }
        while ((hb & 0x80) != 0);

        return true;
    }

    private enum RawObjectTypes : byte
    {
        Invalid1 = 0x00,
        Commit = 0x01,         // OBJ_COMMIT
        Tree = 0x02,           // OBJ_TREE
        Blob = 0x03,           // OBJ_BLOB
        Tag = 0x04,            // OBJ_TAG
        Invalid2 = 0x05,
        OffsetDelta = 0x06,    // OBJ_OFS_DELTA
        ReferenceDelta = 0x07, // OBJ_REF_DELTA
    }

    private async Task<ObjectStreamResult> OpenFromPackedFileAsync(
        string packedFilePath, ulong offset, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. File={packedFilePath}, Offset={offset}, Step={step}");

        var fs = this.fileAccessor.Open(packedFilePath);

        try
        {
            fs.Seek((long)offset, SeekOrigin.Begin);

            var preloadBuffer = new byte[preloadBufferSize];
            var read = await fs.ReadAsync(preloadBuffer, 0, preloadBuffer.Length, ct);
            if (read == 0)
            {
                Throw(1);
            }

            var type = (RawObjectTypes)((preloadBuffer[0] & 0x70) >> 4);

            var preloadIndex = 0;
            if (!TryGetVariableSize(
                preloadBuffer, read, ref preloadIndex, out var objectSize, 4))
            {
                Throw(2);
            }
            if (objectSize > long.MaxValue)
            {
                // Couldn't seek over ceil of int64 on .NET
                Throw(3);
            }

            switch (type)
            {
                case RawObjectTypes.OffsetDelta:
                    {
                        if (!TryGetVariableOffset(
                            preloadBuffer, read, ref preloadIndex, out var referenceRelativeOffset))
                        {
                            Throw(4);
                        }
                        var referenceOffset = offset - referenceRelativeOffset;
                        if (referenceOffset > long.MaxValue)
                        {
                            // Couldn't seek over ceil of int64 on .NET
                            Throw(5);
                        }

                        var stream = new ConcatStream(
                            new PreloadedStream(preloadBuffer, preloadIndex, read - preloadIndex),
                            fs);

                        var (zlibStream, objectEntry) = await Utilities.WhenAll(
                            Utilities.CreateZLibStreamAsync(stream, ct),
                            this.OpenFromPackedFileAsync(packedFilePath, referenceOffset, ct));

                        try
                        {
                            var deltaDecodedStream = await DeltaDecodedStream.CreateAsync(
                                objectEntry.Stream,
                                new RangedStream(zlibStream, (long)objectSize),
                                ct);
                            return new(deltaDecodedStream, objectEntry.Type);
                        }
                        catch
                        {
                            zlibStream.Dispose();
                            objectEntry.Stream.Dispose();
                            throw;
                        }
                    }

                case RawObjectTypes.ReferenceDelta:
                    {
                        if ((preloadIndex + 20) >= read)
                        {
                            Throw(6);
                        }
                        var hashCode = new byte[20];
                        Array.Copy(preloadBuffer, preloadIndex, hashCode, 0, hashCode.Length);
                        preloadIndex += hashCode.Length;
                        var referenceHash = Hash.Create(hashCode);

                        var stream =new ConcatStream(
                            new PreloadedStream(preloadBuffer, preloadIndex, read - preloadIndex),
                            fs);

                        var (zlibStream, objectEntry) = await Utilities.WhenAll(
                            Utilities.CreateZLibStreamAsync(stream, ct),
                            this.OpenAsync(referenceHash, ct));

                        try
                        {
                            if (objectEntry is not { } oe)
                            {
                                throw new InvalidDataException();
                            }

                            var deltaDecodedStream = await DeltaDecodedStream.CreateAsync(
                                oe.Stream,
                                new RangedStream(zlibStream, (long)objectSize),
                                ct);
                            return new(deltaDecodedStream, oe.Type);
                        }
                        catch
                        {
                            zlibStream.Dispose();
                            objectEntry?.Stream.Dispose();
                            throw;
                        }
                    }

                case RawObjectTypes.Commit:
                case RawObjectTypes.Tree:
                case RawObjectTypes.Blob:
                case RawObjectTypes.Tag:
                    {
                        var stream = new RangedStream(
                            new ConcatStream(
                                new PreloadedStream(preloadBuffer, preloadIndex, read - preloadIndex),
                                fs),
                            (long)objectSize);
                        var zlibStream = await Utilities.CreateZLibStreamAsync(stream, ct);
                        return new(zlibStream, (ObjectTypes)(int)type);
                    }

                default:
                    Throw(7);
                    throw new NotImplementedException();
            }
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }

    private async Task<ObjectStreamResult?> OpenFromPackedAsync(
        Hash hash, CancellationToken ct)
    {
        var entries = await Utilities.WhenAll(
            Utilities.EnumerateFiles(this.packedBasePath, "pack-*.idx").
            Select(indexFilePath => this.GetOrCacheIndexEntryAsync(
                indexFilePath.Substring(this.packedBasePath.Length + 1), ct)));

        if (entries.Select(indexEntry =>
            indexEntry.ObjectEntries.TryGetValue(hash, out var objectEntry) ?
            new { indexEntry.BaseFileName, ObjectEntry = objectEntry } : null).
            FirstOrDefault(entry => entry != null) is not { } entry)
        {
            return null;
        }

        var packedFilePath = Utilities.Combine(
            this.packedBasePath,
            entry.BaseFileName + ".pack");

        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. Hash={hash}, File={packedFilePath}, Step={step}");

        if (entry.ObjectEntry.Offset > long.MaxValue)
        {
            Throw(1);
        }

        if (!File.Exists(packedFilePath))
        {
            Throw(2);
        }

        return await this.OpenFromPackedFileAsync(
            packedFilePath, entry.ObjectEntry.Offset, ct);
    }

    //////////////////////////////////////////////////////////////////////////

    public async Task<ObjectStreamResult?> OpenAsync(
        Hash hash, CancellationToken ct)
    {
        var objectPath = Utilities.Combine(
            this.objectsBasePath,
            BitConverter.ToString(hash.HashCode, 0, 1).ToLowerInvariant(),
            BitConverter.ToString(hash.HashCode, 1).Replace("-", string.Empty).ToLowerInvariant());

        if (File.Exists(objectPath))
        {
            return await this.OpenFromObjectFileAsync(objectPath, hash, ct);
        }
        else
        {
            return await this.OpenFromPackedAsync(hash, ct);
        }
    }
}
