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
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GitReader.IO;

internal sealed class FileStreamCache : IDisposable
{
    private sealed class CachedStream : FileStream
    {
        private FileStreamCache parent;
        internal readonly string path;

        public CachedStream(FileStreamCache parent, string path) :
            base(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true)
        {
            this.parent = parent;
            this.path = path;
        }

#if NETSTANDARD1_6
        public void Close()
#else
        public override void Close()
#endif
        {
            if (Interlocked.Exchange(ref this.parent, null!) is { } parent)
            {
                parent.Release(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
            }
        }

        public void CloseExact()
        {
            this.parent = null!;
            base.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    private static readonly int MaxReservedStreams = Environment.ProcessorCount * 2;

    private readonly Dictionary<string, LinkedList<CachedStream>> reserved = new();
    private readonly LinkedList<CachedStream> streamsLRU = new();

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

    private void Release(CachedStream stream)
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

    public Stream Open(string path)
    {
        var fullPath = Path.GetFullPath(path);

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

                return stream;
            }
            else
            {
                if (this.streamsLRU.Count >= MaxReservedStreams)
                {
                    this.RemoveLastReserved();
                }
                return new CachedStream(this, fullPath);
            }
        }
    }
}
