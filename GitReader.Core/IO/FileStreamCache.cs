////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

internal sealed class FileStreamCache : IDisposable
{
    private sealed class CachedStream : Stream
    {
        // This stream is not actually closed when closed from the public interface.
        // The real stream is finally closed when CloseExact() is called,
        // and the timing is managed by FileStreamCache.

        private FileStreamCache parent;
        private Stream rawStream;
        internal readonly string path;

        public CachedStream(FileStreamCache parent, string path, Stream rawStream)
        {
            this.parent = parent;
            this.path = path;
            this.rawStream = rawStream;
        }

        public override bool CanRead =>
            true;
        public override bool CanSeek =>
            true;
        public override bool CanWrite =>
            false;

        public override long Length =>
            this.rawStream.Length;

        public override long Position
        {
            get => this.rawStream.Position;
            set => throw new NotImplementedException();
        }

#if NETSTANDARD1_6
        public void Close()
#else
        public override void Close()
#endif
        {
            if (Interlocked.Exchange(ref this.parent, null!) is { } parent)
            {
                // Temporary (pseudo) closing, this stream will back into cache.
                parent.Return(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
            }
        }

        internal void Resurrect(FileStreamCache parent)
        {
            Debug.Assert(this.parent == null);

            // Re-enabled this stream.
            this.parent = parent;
        }

        internal void CloseExact()
        {
            Debug.Assert(this.parent == null);
            Debug.Assert(this.rawStream != null);

            // Completely discards this stream.
            this.parent = null!;
            base.Dispose(true);
            this.rawStream!.Dispose();
            this.rawStream = null!;
            GC.SuppressFinalize(this);
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            this.rawStream.Read(buffer, offset, count);

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken ct) =>
            this.rawStream.ReadAsync(buffer, offset, count, ct);
#endif

        public override long Seek(long offset, SeekOrigin origin) =>
            this.rawStream.Seek(offset, origin);

        public override void SetLength(long value) =>
             this.rawStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();

        public override void Flush() =>
            throw new NotImplementedException();
    }

    internal static readonly int MaxReservedStreams = Environment.ProcessorCount * 2;

    private readonly IFileSystem fileSystem;
    private readonly Dictionary<string, LinkedList<CachedStream>> reserved = new();
    private readonly LinkedList<CachedStream> streamsLRU = new();

    public FileStreamCache(IFileSystem fileSystem) =>
        this.fileSystem = fileSystem;

    public void Dispose()
    {
        lock (this.reserved)
        {
            while (this.streamsLRU.Count >= 1)
            {
                this.RemoveLastReserved();
            }

            Debug.Assert(this.reserved.Count == 0);

            this.reserved.Clear();
        }
    }

    private void RemoveLastReserved()
    {
        if (this.streamsLRU.Last?.Value is { } stream)
        {
            this.streamsLRU.RemoveLast();

            var streams = this.reserved[stream.path];
            var rd = streams.Remove(stream);
            Debug.Assert(rd);

            if (streams.Count == 0)
            {
                this.reserved.Remove(stream.path);
            }

            stream.CloseExact();
        }
    }

    private void Return(CachedStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        lock (this.reserved)
        {
            if (!this.reserved.TryGetValue(stream.path, out var streams))
            {
                streams = new();
                this.reserved.Add(stream.path, streams);
            }

            streams.AddFirst(stream);
            this.streamsLRU.AddFirst(stream);
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public async ValueTask<Stream> OpenAsync(
        string path, CancellationToken ct)
#else
    public async Task<Stream> OpenAsync(
        string path, CancellationToken ct)
#endif
    {
        var fullPath = this.fileSystem.GetFullPath(path);

        lock (this.reserved)
        {
            if (this.reserved.TryGetValue(fullPath, out var streams))
            {
                var stream = streams.First!.Value;
                Debug.Assert(stream.Position == 0);

                streams.RemoveFirst();
                if (streams.Count == 0)
                {
                    this.reserved.Remove(fullPath);
                }

                var rd = this.streamsLRU.Remove(stream);
                Debug.Assert(rd);

                stream.Resurrect(this);

                return stream;
            }
            else
            {
                if (this.streamsLRU.Count >= MaxReservedStreams)
                {
                    this.RemoveLastReserved();
                }
            }
        }

        var stream2 = await this.fileSystem.OpenAsync(path, true, ct);

        return new CachedStream(this, path, stream2);
    }
}
