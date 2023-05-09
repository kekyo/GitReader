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
    public static async Task<PrimitiveRepository> OpenPrimitiveAsync(
        string path, CancellationToken ct)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var repository = new PrimitiveRepository(repositoryPath);

        try
        {
            // Read remote references from config file.
            repository.remoteUrls =
                await RepositoryAccessor.ReadRemoteReferencesAsync(repository, ct);

            // Read FETCH_HEAD and packed-refs.
            var (fhc1, fhc2) = await Utilities.Join(
                RepositoryAccessor.ReadFetchHeadsAsync(repository, ct),
                RepositoryAccessor.ReadPackedRefsAsync(repository, ct));
            repository.referenceCache = fhc1.Combine(fhc2);

            return repository;
        }
        catch
        {
            repository.Dispose();
            throw;
        }
    }

    //////////////////////////////////////////////////////////////////////////

    public static async Task<PrimitiveReference?> GetCurrentHeadReferenceAsync(
        Repository repository,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        return results is { } r ?
            new PrimitiveReference(r.Names.Last(), r.Hash) :
            null;
    }

    public static async Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        Repository repository,
        string branchName,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/heads/{branchName}", ct);
        return results is { } r ?
            new PrimitiveReference(branchName, r.Hash) :
            throw new ArgumentException($"Could not find a branch: {branchName}");
    }

    public static async Task<PrimitiveReference> GetRemoteBranchHeadReferenceAsync(
        Repository repository,
        string branchName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/remotes/{branchName}", ct);
        return results is { } r ?
            new PrimitiveReference(branchName, r.Hash) :
            throw new ArgumentException($"Could not find a remote branch: {branchName}");
    }

    public static async Task<PrimitiveReference> GetTagReferenceAsync(
        Repository repository,
        string tagName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/tags/{tagName}", ct);
        return results is { } r ?
            new PrimitiveReference(tagName, r.Hash) :
            throw new ArgumentException($"Could not find a tag: {tagName}");
    }
}
