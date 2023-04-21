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
    private TemporaryFile locker;
    internal ObjectAccessor accessor;

    internal Repository(
        string repositoryPath, TemporaryFile locker)
    {
        this.Path = repositoryPath;
        this.accessor = new(repositoryPath);
        this.locker = locker;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.accessor, null!) is { } accessor)
        {
            accessor.Dispose();
        }
        if (Interlocked.Exchange(ref this.locker, null!) is { } locker)
        {
            locker.Dispose();
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
