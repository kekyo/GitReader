////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
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
    public static async Task<Reference> GetCurrentHeadAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var results = await repository.ReadHashAsync("HEAD", ct);
        return Reference.Create(results.Names.Last(), results.Hash);
    }

    public static async Task<Reference> GetBranchHeadAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default)
    {
        var results = await repository.ReadHashAsync($"refs/heads/{branchName}", ct);
        return Reference.Create(branchName, results.Hash);
    }

    public static Task<Commit> GetCommitAsync(
        this Repository repository,
        Hash commit, CancellationToken ct = default) =>
        repository.ReadCommitAsync(commit, ct);

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
                var results = await repository.ReadHashAsync(
                    path.Substring(repository.Path.Length + 1),
                    ct);
                return Reference.Create(
                    path.Substring(headsPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                    results.Hash);
            }));
        return branches;
    }

    public static Task<Reference[]> GetBranchHeadsAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "heads", ct);

    public static Task<Reference[]> GetRemoteBranchHeadsAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "remotes", ct);

    public static Task<Reference[]> GetTagsAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        GetReferencesAsync(repository, "tags", ct);
}
