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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

internal static class RepositoryFacade
{
    public static Task<Repository> OpenPrimitiveAsync(
        string path, CancellationToken ct)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        return Utilities.FromResult(new Repository(repositoryPath));
    }

    public static async Task<Reference?> GetCurrentHeadReferenceAsync(
        Repository repository,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        return results is { } r ?
            Reference.Create(r.Names.Last(), r.Hash) :
            null;
    }

    public static async Task<Reference> GetBranchHeadReferenceAsync(
        Repository repository,
        string branchName,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/heads/{branchName}", ct);
        return results is { } r ?
            Reference.Create(branchName, r.Hash) :
            throw new ArgumentException($"Could not find a branch: {branchName}");
    }

    public static async Task<Reference> GetRemoteBranchHeadReferenceAsync(
        Repository repository,
        string branchName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/remotes/{branchName}", ct);
        return results is { } r ?
            Reference.Create(branchName, r.Hash) :
            throw new ArgumentException($"Could not find a remote branch: {branchName}");
    }

    public static async Task<Reference> GetTagReferenceAsync(
        Repository repository,
        string tagName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/tags/{tagName}", ct);
        return results is { } r ?
            Reference.Create(tagName, r.Hash) :
            throw new ArgumentException($"Could not find a tag: {tagName}");
    }
}
