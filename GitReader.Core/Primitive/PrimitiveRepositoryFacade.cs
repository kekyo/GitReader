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

internal static class PrimitiveRepositoryFacade
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
        var relativePathOrLocation = "HEAD";
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, relativePathOrLocation, ct);
        return results is { } r ?
            new PrimitiveReference(r.Names.Last(), relativePathOrLocation, r.Hash) :
            null;
    }

    public static async Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        Repository repository,
        string branchName,
        CancellationToken ct)
    {
        var path = $"refs/heads/{branchName}";
        var remotePath = $"refs/remotes/{branchName}";
        var (results, remoteResults) = await Utilities.Join(
            RepositoryAccessor.ReadHashAsync(repository, path, ct),
            RepositoryAccessor.ReadHashAsync(repository, remotePath, ct));
        return results is { } r ?
            new PrimitiveReference(branchName, path, r.Hash) :
            remoteResults is { } rr ?
            new PrimitiveReference(branchName, remotePath, rr.Hash) :
            throw new ArgumentException($"Could not find a branch: {branchName}");
    }

    public static async Task<PrimitiveReference> GetTagReferenceAsync(
        Repository repository,
        string tagName, CancellationToken ct)
    {
        var relativePathOrLocation = $"refs/tags/{tagName}";
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, relativePathOrLocation, ct);
        return results is { } r ?
            new PrimitiveReference(tagName, relativePathOrLocation, r.Hash) :
            throw new ArgumentException($"Could not find a tag: {tagName}");
    }
}
