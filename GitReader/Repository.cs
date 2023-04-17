////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
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
    private Locker locker;

    private Repository(
        string path, Locker locker)
    {
        this.Path = path;
        this.locker = locker;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.locker, null!) is { } locker)
        {
            locker.Dispose();
        }
    }

    public string Path { get; }

    public static async Task<Repository> OpenAsync(
        string repositoryPath, CancellationToken ct = default, bool forceUnlock = false)
    {
        var path = System.IO.Path.GetFileName(repositoryPath) != ".git" ?
            Utilities.Combine(repositoryPath, ".git") : repositoryPath;

        if (!Directory.Exists(path))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var lockPath = Utilities.Combine(path, "index.lock");
        var locker = await Locker.CreateAsync(lockPath, ct, forceUnlock);

        return new(path, locker);
    }
}
