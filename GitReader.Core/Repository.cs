﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using GitReader.IO;
using System;
using System.Threading;

namespace GitReader;

public abstract class Repository : IDisposable
{
    internal BufferPool pool = new();
    internal IFileSystem fileSystem;
    internal FileStreamCache fileStreamCache;
    internal ObjectAccessor objectAccessor;
    internal ReadOnlyDictionary<string, string> remoteUrls = null!;
    internal ReferenceCache referenceCache;

    private protected Repository(
        string gitPath,
        IFileSystem fileSystem)
    {
        this.GitPath = gitPath;
        this.fileSystem = fileSystem;
        this.fileStreamCache = new(this.fileSystem);
        this.objectAccessor = new(this.pool, this.fileSystem, this.fileStreamCache, gitPath);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.objectAccessor, null!) is { } objectAccessor)
        {
            objectAccessor.Dispose();
        }
        if (Interlocked.Exchange(ref this.fileStreamCache, null!) is { } fileStreamCache)
        {
            fileStreamCache.Dispose();
        }
    }

    public string GitPath { get; }

    public ReadOnlyDictionary<string, string> RemoteUrls =>
        this.remoteUrls;

#if DEBUG
    public int HitCount =>
        this.objectAccessor.hitCount;
    public int MissCount =>
        this.objectAccessor.missCount;
#endif

    public static readonly RepositoryFactory Factory = new();
}

public sealed class RepositoryFactory
{
    internal RepositoryFactory()
    {
    }
}
