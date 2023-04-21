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

namespace GitReader.Structures;

public static class RepositoryFactoryExtension
{
    public static async Task<StructuredRepository> OpenAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default, bool forceUnlock = false)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
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
