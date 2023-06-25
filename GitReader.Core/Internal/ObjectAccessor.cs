////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal sealed class ObjectAccessor : IDisposable
{
    private const int preloadBufferSize = 65536;
    private static readonly int maxStreamCache = 16;

    private sealed class ObjectStreamCacheHolder
    {
        public readonly string Path;
        public readonly ulong Offset;
        public readonly ObjectTypes Type;
        public readonly WrappedStream Stream;
        public DateTime Limit;
#if DEBUG
        public int HitCount;
#endif
        public ObjectStreamCacheHolder(
            string path, ulong offset, ObjectTypes type,
            WrappedStream stream, DateTime limit)
        {
            this.Path = path;
            this.Offset = offset;
            this.Type = type;
            this.Stream = stream;
            this.Limit = limit;
        }
    }

    private readonly FileStreamCache fileStreamCache;
    private readonly string objectsBasePath;
    private readonly string packedBasePath;
    private readonly AsyncLock locker = new();
    private readonly Dictionary<string, IndexEntry> indexCache = new();
    private readonly LinkedList<ObjectStreamCacheHolder> streamLRUCache = new();
    private readonly Timer streamLRUCacheExhaustTimer;
#if DEBUG
    internal int hitCount;
    internal int missCount;
#endif

    public ObjectAccessor(
        FileStreamCache fileStreamCache, string gitPath)
    {
        this.fileStreamCache = fileStreamCache;
        this.objectsBasePath = Utilities.Combine(
            gitPath,
            "objects");
        this.packedBasePath = Utilities.Combine(
            this.objectsBasePath,
            "pack");
        this.streamLRUCacheExhaustTimer =
            new(this.ExhaustStreamCache, null,
                Utilities.Infinite, Utilities.Infinite);
    }

    public void Dispose()
    {
        using var _ = this.locker.Lock();

        this.indexCache.Clear();

        lock (this.streamLRUCache)
        {
            this.streamLRUCacheExhaustTimer.Change(
                Utilities.Infinite, Utilities.Infinite);
            this.streamLRUCacheExhaustTimer.Dispose();

            while (this.streamLRUCache.First is { } holder)
            {
                holder.Value.Stream.Dispose();
                this.streamLRUCache.Remove(holder);
#if DEBUG
                if (holder.Value.HitCount >= 1)
                {
                    this.hitCount += holder.Value.HitCount;
                }
                else
                {
                    this.missCount++;
                }
#endif
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<ObjectStreamResult?> OpenFromObjectFileAsync(
        string objectPath, Hash hash, CancellationToken ct)
#else
    private async Task<ObjectStreamResult?> OpenFromObjectFileAsync(
        string objectPath, Hash hash, CancellationToken ct)
#endif
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. Hash={hash}, Step={step}");

        var fs = this.fileStreamCache.Open(objectPath);

        try
        {
            var zlibStream = await ZLibStream.CreateAsync(fs, ct);

            using var preloadBuffer = BufferPool.Take(preloadBufferSize);
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
                    new PreloadedStream(preloadBuffer.Detach(), preloadIndex, read),
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<IndexEntry> GetOrCacheIndexEntryAsync(
        string indexFileRelativePath, CancellationToken ct)
#else
    private async Task<IndexEntry> GetOrCacheIndexEntryAsync(
        string indexFileRelativePath, CancellationToken ct)
#endif
    {
        using var _ = await this.locker.LockAsync(ct);

        if (this.indexCache.TryGetValue(indexFileRelativePath, out var cachedEntry))
        {
            return cachedEntry;
        }

        var dict = await IndexReader.ReadIndexAsync(
            Utilities.Combine(this.packedBasePath, indexFileRelativePath), ct);
        cachedEntry = new(
            Utilities.Combine(
                Utilities.GetDirectoryPath(indexFileRelativePath),
                Path.GetFileNameWithoutExtension(indexFileRelativePath)),
            dict);

        this.indexCache[indexFileRelativePath] = cachedEntry;

        return cachedEntry;
    }

    //////////////////////////////////////////////////////////////////////////

    // https://git-scm.com/docs/pack-format

    public static bool TryGetVariableSize(
        byte[] buffer, int bufferCount, ref int index, out ulong value, int initialBits)
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

    private void ExhaustStreamCache(object? _)
    {
        var now = DateTime.Now;

        lock (this.streamLRUCache)
        {
            var holder = this.streamLRUCache.First;
            while (holder != null)
            {
                if (holder.Value.Limit <= now)
                {
                    holder.Value.Stream.Dispose();  // [1]
                    this.streamLRUCache.Remove(holder);
                }

                holder = holder.Next;
            }

            if (this.streamLRUCache.Count >= 1)
            {
                var dueTime = this.streamLRUCache.First!.Value.Limit - now;
                if (dueTime < TimeSpan.Zero)
                {
                    dueTime = TimeSpan.Zero;
                }

                this.streamLRUCacheExhaustTimer.Change(
                    dueTime, Utilities.Infinite);
            }
            else
            {
                this.streamLRUCacheExhaustTimer.Change(
                    Utilities.Infinite, Utilities.Infinite);
            }
        }
    }

    private Stream AddToCache(
        string packedFilePath, ulong offset, ObjectTypes type,
        Stream stream, bool disableCaching)
    {
        if (disableCaching || maxStreamCache <= 1)
        {
            return stream;
        }

        var heldStream = new WrappedStream(stream);
        var cachedStream = heldStream.Clone();   // [1]

        var now = DateTime.Now;
        var limit = now.Add(TimeSpan.FromSeconds(10));

        lock (this.streamLRUCache)
        {
            this.streamLRUCache.AddFirst(
                new ObjectStreamCacheHolder(packedFilePath, offset, type, heldStream, limit));

            while (this.streamLRUCache.Count > maxStreamCache)
            {
                var holder = this.streamLRUCache.Last!;
                holder.Value.Stream.Dispose();
                this.streamLRUCache.Remove(holder);
#if DEBUG
                if (holder.Value.HitCount >= 1)
                {
                    this.hitCount += holder.Value.HitCount;
                }
                else
                {
                    this.missCount++;
                }
#endif
            }

            if (this.streamLRUCache.Count >= 1)
            {
                var dueTime = this.streamLRUCache.First!.Value.Limit - now;
                if (dueTime < TimeSpan.Zero)
                {
                    dueTime = TimeSpan.Zero;
                }

                // Race condition [1]:
                // If dueTime is zero here, ExhaustStreamCache is called directly in same thread context.
                // Within ExhaustStreamCache, the heldStream will be released,
                // when it returns here, the counter of the stream may be 0.
                // Therefore, cachedStreams for return are cloned in advance so that the counter does not become 0.
                this.streamLRUCacheExhaustTimer.Change(
                    dueTime, Utilities.Infinite);
            }
        }

        return cachedStream;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<ObjectStreamResult> OpenFromPackedFileAsync(
        string packedFilePath, ulong offset, bool disableCaching, CancellationToken ct)
#else
    private async Task<ObjectStreamResult> OpenFromPackedFileAsync(
        string packedFilePath, ulong offset, bool disableCaching, CancellationToken ct)
#endif
    {
        if (maxStreamCache >= 2)
        {
            var dueTime = TimeSpan.FromSeconds(10);
            var limit = DateTime.Now.Add(dueTime);

            lock (this.streamLRUCache)
            {
                var holder = this.streamLRUCache.First;
                while (holder != null)
                {
                    if (holder.Value.Path == packedFilePath &&
                        holder.Value.Offset == offset)
                    {
                        this.streamLRUCache.Remove(holder);
                        this.streamLRUCache.AddFirst(holder);

                        holder.Value.Limit = limit;
#if DEBUG
                        Interlocked.Increment(ref holder.Value.HitCount);
#endif
                        this.streamLRUCacheExhaustTimer.Change(
                            dueTime, Utilities.Infinite);

                        return new(holder.Value.Stream.Clone(), holder.Value.Type);
                    }

                    holder = holder.Next;
                }
            }
        }

        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. File={packedFilePath}, Offset={offset}, Step={step}");

        var fs = this.fileStreamCache.Open(packedFilePath);

        try
        {
            fs.Seek((long)offset, SeekOrigin.Begin);

            using var preloadBuffer = BufferPool.Take(preloadBufferSize);
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
                            new PreloadedStream(preloadBuffer.Detach(), preloadIndex, read),
                            fs);

                        var (zlibStream, objectEntry) = await Utilities.Join(
                            ZLibStream.CreateAsync(stream, ct),
                            this.OpenFromPackedFileAsync(packedFilePath, referenceOffset, disableCaching, ct));

                        try
                        {
                            var deltaDecodedStream = await DeltaDecodedStream.CreateAsync(
                                objectEntry.Stream,
                                new RangedStream(zlibStream, (long)objectSize),
                                ct);

                            var wrappedStream = this.AddToCache(
                                packedFilePath, offset, objectEntry.Type,
                                await MemoizedStream.CreateAsync(deltaDecodedStream, -1, ct),
                                disableCaching);

                            return new(wrappedStream, objectEntry.Type);
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

                        using var hashCode = BufferPool.Take(20);
                        Array.Copy(preloadBuffer, preloadIndex, hashCode, 0, hashCode.Length);
                        preloadIndex += hashCode.Length;
                        var referenceHash = new Hash(hashCode);

                        var stream = new ConcatStream(
                            new PreloadedStream(preloadBuffer.Detach(), preloadIndex, read),
                            fs);

                        var (zlibStream, objectEntry) = await Utilities.Join(
                            ZLibStream.CreateAsync(stream, ct),
                            this.OpenAsync(referenceHash, disableCaching, ct));

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

                            var wrappedStream = this.AddToCache(
                                packedFilePath, offset, oe.Type,
                                await MemoizedStream.CreateAsync(deltaDecodedStream, -1, ct),
                                disableCaching);

                            return new(wrappedStream, oe.Type);
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
                        var stream = new ConcatStream(
                            new PreloadedStream(preloadBuffer.Detach(), preloadIndex, read),
                            fs);

                        var zlibStream = await ZLibStream.CreateAsync(stream, ct);
                        var objectType = (ObjectTypes)(int)type;

                        var wrappedStream = this.AddToCache(
                            packedFilePath, offset, objectType,
                            await MemoizedStream.CreateAsync(zlibStream, (long)objectSize, ct),
                            disableCaching);

                        return new(wrappedStream, objectType);
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<ObjectStreamResult?> OpenFromPackedAsync(
        Hash hash, bool disableCaching, CancellationToken ct)
#else
    private async Task<ObjectStreamResult?> OpenFromPackedAsync(
        Hash hash, bool disableCaching, CancellationToken ct)
#endif
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
            packedFilePath, entry.ObjectEntry.Offset, disableCaching, ct);
    }

    //////////////////////////////////////////////////////////////////////////

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<ObjectStreamResult?> OpenAsync(
        Hash hash, bool disableCaching, CancellationToken ct)
#else
    public Task<ObjectStreamResult?> OpenAsync(
        Hash hash, bool disableCaching, CancellationToken ct)
#endif
    {
        var objectPath = Utilities.Combine(
            this.objectsBasePath,
            BitConverter.ToString(hash.HashCode, 0, 1).ToLowerInvariant(),
            BitConverter.ToString(hash.HashCode, 1).Replace("-", string.Empty).ToLowerInvariant());

        if (File.Exists(objectPath))
        {
            return this.OpenFromObjectFileAsync(objectPath, hash, ct);
        }
        else
        {
            return this.OpenFromPackedAsync(hash, disableCaching, ct);
        }
    }
}
