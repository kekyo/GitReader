////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using GitReader.Primitive;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

public static class RepositoryExtension
{
    public static async Task<Commit> GetCurrentHeadAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, results.Hash, ct);
        return new(repository, commit);
    }

    public static int GetParentCount(
        this Commit commit) =>
        commit.parents.Length;

    public static async Task<Commit> GetParentAsync(
        this Commit commit,
        int index, CancellationToken ct = default)
    {
        var parent = await RepositoryAccessor.ReadCommitAsync(
            commit.repository, commit.parents[index], ct);
        return new(commit.repository, parent);
    }

    public static async Task<Branch> GetBranchAsync(
        this Repository repository,
        string branchName,
        CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/heads/{branchName}", ct);
        var head = await RepositoryAccessor.ReadCommitAsync(
            repository, results.Hash, ct);
        return new(branchName, new(repository, head));
    }

    public static async Task<Branch> GetRemoteBranchAsync(
        this Repository repository,
        string branchName,
        CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/remotes/{branchName}", ct);
        var head = await RepositoryAccessor.ReadCommitAsync(
            repository, results.Hash, ct);
        return new(branchName, new(repository, head));
    }

    public static async Task<Tag> GetTagAsync(
        this Repository repository,
        string tagName,
        CancellationToken ct = default)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/tags/{tagName}", ct);
        var tag = await RepositoryAccessor.ReadTagAsync(
            repository, results.Hash, ct);
        return tag is { } t ?
            new(t) : new Tag(results.Hash, ObjectTypes.Commit, tagName);
    }

    public static async Task<Branch[]> GetBranchesAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, "heads", ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await RepositoryAccessor.ReadCommitAsync(
                    repository, reference.Target, ct)
            }));
        return entries.
            Select(entry => new Branch(entry.Name, new(repository, entry.Head))).
            ToArray();
    }

    public static async Task<Branch[]> GetRemoteBranchesAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, "remotes", ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await RepositoryAccessor.ReadCommitAsync(
                    repository, reference.Target, ct)
            }));
        return entries.
            Select(entry => new Branch(entry.Name, new(repository, entry.Head))).
            ToArray();
    }

    public static async Task<Tag[]> GetTagsAsync(
        this Repository repository,
        CancellationToken ct = default)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, "tags", ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Hash = reference.Target,
                Name = reference.Name,
                Tag = await RepositoryAccessor.ReadTagAsync(
                    repository, reference.Target, ct)
            }));
        return entries.
            Select(entry => entry.Tag is { } tag ?
                new Tag(tag) : new Tag(entry.Hash, ObjectTypes.Commit, entry.Name)).
            ToArray();
    }
}
