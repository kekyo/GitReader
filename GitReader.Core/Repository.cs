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
using System.Collections.Generic;
using System.Threading;

namespace GitReader;

public class Repository : IDisposable
{
    internal ObjectAccessor accessor;
    internal RemoteReferenceCache? remoteReferenceCache;
    internal FetchHeadCache? fetchHeadCache;

    internal Repository(
        string repositoryPath)
    {
        this.Path = repositoryPath;
        this.accessor = new(repositoryPath);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.accessor, null!) is { } accessor)
        {
            accessor.Dispose();
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
