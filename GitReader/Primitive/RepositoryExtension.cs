////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

public static class RepositoryExtension
{
    public static async Task<Reference> GetCurrentHeadReferenceAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        return Reference.Create(results.Names.Last(), results.Hash);
    }

    public static async Task<Reference> GetBranchHeadReferenceAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/heads/{branchName}", ct);
        return Reference.Create(branchName, results.Hash);
    }

    public static Task<Commit> GetCommitAsync(
        this Repository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryAccessor.ReadCommitAsync(repository, commit, ct);

    public static async Task<Tag> GetTagAsync(
        this Repository repository,
        Reference tag, CancellationToken ct = default) =>
        await RepositoryAccessor.ReadTagAsync(repository, tag, ct) is { } t ?
            t : Tag.Create(tag, ObjectTypes.Commit, tag.Name, null, null);

    private static async Task<Reference[]> GetReferencesAsync(
        Repository repository,
        string type,
        CancellationToken ct)
    {
        var headsPath = Utilities.Combine(
            repository.Path, "refs", type);
        var branches = await Utilities.WhenAll(
            Utilities.EnumerateFiles(headsPath, "*").
            Select(async path =>
            {
                var results = await RepositoryAccessor.ReadHashAsync(
                    repository,
                    path.Substring(repository.Path.Length + 1),
                    ct);
                return Reference.Create(
                    path.Substring(headsPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                    results.Hash);
            }));
        return branches;
    }

    public static Task<Reference[]> GetBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "heads", ct);

    public static Task<Reference[]> GetRemoteBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "remotes", ct);

    public static Task<Reference[]> GetTagReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "tags", ct);
}
