////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.Threading;

namespace GitReader;

public class Repository : IDisposable
{
    internal FileAccessor fileAccessor = new();
    internal ObjectAccessor objectAccessor;
    internal RemoteReferenceUrlCache remoteReferenceUrlCache;
    internal ReferenceCache referenceCache;

    internal Repository(
        string repositoryPath)
    {
        this.Path = repositoryPath;
        this.objectAccessor = new(this.fileAccessor, repositoryPath);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.objectAccessor, null!) is { } objectAccessor)
        {
            objectAccessor.Dispose();
        }
        if (Interlocked.Exchange(ref this.fileAccessor, null!) is { } fileAccessor)
        {
            fileAccessor.Dispose();
        }
    }

    public string Path { get; }

    public static readonly RepositoryFactory Factory = new();
}

public sealed class RepositoryFactory
{
    internal RepositoryFactory()
    {
    }
}
