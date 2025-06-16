////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using GitReader.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader;

/// <summary>
/// Represents a base class for Git repository access.
/// </summary>
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
        string[] alternativeGitPaths,
        IFileSystem fileSystem)
    {
        this.GitPath = gitPath;
        this.TryingPathList = [..alternativeGitPaths, gitPath];
        this.fileSystem = fileSystem;
        this.fileStreamCache = new(this.fileSystem);
        this.objectAccessor = new(this.pool, this.fileSystem, this.fileStreamCache, gitPath);
    }

    /// <summary>
    /// Disposes the repository and releases all resources.
    /// </summary>
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

    /// <summary>
    /// Gets the path to the Git repository.
    /// </summary>
    public string GitPath { get; }

    /// <summary>
    /// Gets the list of paths that were tried when opening the repository.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string[] TryingPathList { get; }

    /// <summary>
    /// Gets the remote URLs configured for this repository.
    /// </summary>
    public ReadOnlyDictionary<string, string> RemoteUrls =>
        this.remoteUrls;

#if DEBUG
    /// <summary>
    /// Gets the number of cache hits (debug builds only).
    /// </summary>
    public int HitCount =>
        this.objectAccessor.hitCount;
    
    /// <summary>
    /// Gets the number of cache misses (debug builds only).
    /// </summary>
    public int MissCount =>
        this.objectAccessor.missCount;
#endif

    /// <summary>
    /// Gets the default repository factory instance.
    /// </summary>
    public static readonly RepositoryFactory Factory = new();
}

/// <summary>
/// Provides factory methods for creating repository instances.
/// </summary>
public sealed class RepositoryFactory
{
    internal RepositoryFactory()
    {
    }
}
