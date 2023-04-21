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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader;

public sealed class Repository : IDisposable
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

public static class RepositoryFactoryExtension
{
    public static async Task<Repository> OpenAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default, bool forceUnlock = false)
    {
        var repositoryPath = System.IO.Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var lockPath = Utilities.Combine(repositoryPath, "index.lock");
        var locker = await TemporaryFile.CreateLockFileAsync(lockPath, ct, forceUnlock);

        return new(repositoryPath, locker);
    }
}
