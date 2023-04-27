////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

internal static class RepositoryFacade
{
    private static async Task<Branch?> GetCurrentHeadAsync(
        StructuredRepository repository,
        CancellationToken ct)
    {
        if (await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct) is { } results)
        {
            var commit = await RepositoryAccessor.ReadCommitAsync(
                repository, results.Hash, ct);
            return commit is { } c ?
                new(results.Names.Last(), new(new(repository), c)) :
                throw new InvalidDataException(
                    $"Could not find a commit: {results.Hash}");
        }
        else
        {
            return null;
        }
    }

    private static async Task<ReadOnlyDictionary<string, Branch>> GetStructuredBranchesAsync(
        Structures.StructuredRepository repository,
        string baseName,
        CancellationToken ct)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, baseName, ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await RepositoryAccessor.ReadCommitAsync(
                    repository, reference.Target, ct)
            }));
        return new(entries.
            Where(entry => entry.Head.HasValue).
            ToDictionary(
                entry => entry.Name,
                entry => new Branch(entry.Name, new(new(repository), entry.Head!.Value))));
    }

    private static async Task<ReadOnlyDictionary<string, Tag>> GetStructuredTagsAsync(
        StructuredRepository repository,
        CancellationToken ct)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, "tags", ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Tag = (await RepositoryAccessor.ReadTagAsync(
                    repository, reference.Target, ct)) is { } tag ?
                        new Tag(tag) :
                        new Tag(reference.Target, ObjectTypes.Commit, reference.Name),
            }));
        return new(entries.ToDictionary(
            entry => entry.Name,
            entry => entry.Tag));
    }

    public static async Task<StructuredRepository> OpenStructuredAsync(
        string path, CancellationToken ct)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var repository = new StructuredRepository(repositoryPath);
        try
        {
            var (head, branches, remoteBranches, tags) = await Utilities.WhenAll(
                GetCurrentHeadAsync(repository, ct),
                GetStructuredBranchesAsync(repository, "heads", ct),
                GetStructuredBranchesAsync(repository, "remotes", ct),
                GetStructuredTagsAsync(repository, ct));

            repository.head = head;
            repository.branches = branches;
            repository.remoteBranches = remoteBranches;
            repository.tags = tags;

            return repository;
        }
        catch
        {
            repository.Dispose();
            throw;
        }
    }

    public static async Task<Commit?> GetCommitDirectlyAsync(
        StructuredRepository repository,
        Hash hash,
        CancellationToken ct)
    {
        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, hash, ct);
        return commit is { } c ?
            new(new(repository), c) : null;
    }

    private static StructuredRepository GetRelatedRepository(
        Commit commit)
    {
        if (commit.rwr.Target is not StructuredRepository repository ||
            repository.accessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }
        return repository;
    }

    public static async Task<Commit?> GetPrimaryParentAsync(
        Commit commit,
        CancellationToken ct)
    {
        if (commit.parents.Length == 0)
        {
            return null;
        }

        var repository = GetRelatedRepository(commit);
        var pc = await RepositoryAccessor.ReadCommitAsync(
            repository, commit.parents[0], ct);
        return pc is { } ?
            new(commit.rwr, pc!.Value) :
            throw new InvalidDataException(
                $"Could not find a commit: {commit.parents[0]}");
    }

    public static Task<Commit[]> GetParentsAsync(
        Commit commit,
        CancellationToken ct)
    {
        var repository = GetRelatedRepository(commit);

        return Utilities.WhenAll(
            commit.parents.Select(async parent =>
            {
                var pc = await RepositoryAccessor.ReadCommitAsync(
                    repository, parent, ct);
                return pc is { } ?
                    new Commit(commit.rwr, pc!.Value) :
                    throw new InvalidDataException(
                        $"Could not find a commit: {parent}");
            }));
    }

    public static Branch[] GetRelatedBranches(Commit commit)
    {
        var repository = GetRelatedRepository(commit);
        return repository.Branches.Values.
            Collect(branch => branch.Head.Equals(commit) ? branch : null).
            ToArray();
    }

    public static Branch[] GetRelatedRemoteBranches(Commit commit)
    {
        var repository = GetRelatedRepository(commit);
        return repository.RemoteBranches.Values.
            Collect(branch => branch.Head.Equals(commit) ? branch : null).
            ToArray();
    }

    public static Tag[] GetRelatedTags(Commit commit)
    {
        var repository = GetRelatedRepository(commit);
        return repository.Tags.Values.
            Collect(tag =>
                (tag.Type == ObjectTypes.Commit &&
                 tag.Hash.Equals(commit.Hash)) ? tag : null).
            ToArray();
    }
}
