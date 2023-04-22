////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class RepositoryFacade
{
    public static async Task<Repository> OpenPrimitiveAsync(
        string path, CancellationToken ct, bool forceUnlock)
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

    public static async Task<Primitive.Reference> GetCurrentHeadReferenceAsync(
        Repository repository,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        return Primitive.Reference.Create(results.Names.Last(), results.Hash);
    }

    public static async Task<Primitive.Reference> GetBranchHeadReferenceAsync(
        Repository repository,
        string branchName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/heads/{branchName}", ct);
        return Primitive.Reference.Create(branchName, results.Hash);
    }

    public static async Task<Primitive.Reference> GetRemoteBranchHeadReferenceAsync(
        Repository repository,
        string branchName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/remotes/{branchName}", ct);
        return Primitive.Reference.Create(branchName, results.Hash);
    }

    public static async Task<Primitive.Reference> GetTagReferenceAsync(
        Repository repository,
        string tagName, CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, $"refs/tags/{tagName}", ct);
        return Primitive.Reference.Create(tagName, results.Hash);
    }

    //////////////////////////////////////////////////////////////////////////

    private static async Task<Structures.Commit> GetCurrentHeadAsync(
        Structures.StructuredRepository repository,
        CancellationToken ct)
    {
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct);
        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, results.Hash, ct);
        return new(new(repository), commit);
    }

    private static async Task<ReadOnlyDictionary<string, Structures.Branch>> GetStructuredBranchesAsync(
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
        return entries.ToDictionary(
            entry => entry.Name,
            entry => new Structures.Branch(entry.Name, new(new(repository), entry.Head))).
            AsReadOnly();
    }

    private static async Task<ReadOnlyDictionary<string, Structures.Tag>> GetStructuredTagsAsync(
        Structures.StructuredRepository repository,
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
                        new Structures.Tag(tag) :
                        new Structures.Tag(reference.Target, ObjectTypes.Commit, reference.Name),
            }));
        return entries.ToDictionary(
            entry => entry.Name,
            entry => entry.Tag).
            AsReadOnly();
    }

    public static async Task<Structures.StructuredRepository> OpenStructuredAsync(
        string path, CancellationToken ct, bool forceUnlock)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var lockPath = Utilities.Combine(repositoryPath, "index.lock");
        var locker = await TemporaryFile.CreateLockFileAsync(lockPath, ct, forceUnlock);

        try
        {
            var repository = new Structures.StructuredRepository(repositoryPath, locker);

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
            locker.Dispose();
            throw;
        }
    }

    public static async Task<Structures.Commit> GetCommitDirectlyAsync(
        Structures.StructuredRepository repository,
        Hash hash,
        CancellationToken ct)
    {
        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, hash, ct);
        return new(new(repository), commit);
    }

    private static Structures.StructuredRepository GetRepositoryFrom(
        Structures.Commit commit)
    {
        if (commit.rwr.Target is not Structures.StructuredRepository repository ||
            repository.accessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }
        return repository;
    }

    public static Task<Structures.Commit[]> GetParentsAsync(
        Structures.Commit commit,
        CancellationToken ct)
    {
        var repository = GetRepositoryFrom(commit);

        return Utilities.WhenAll(
            commit.parents.Select(async parent =>
            {
                var pc = await RepositoryAccessor.ReadCommitAsync(
                    repository, parent, ct);
                return new Structures.Commit(commit.rwr, pc);
            }));
    }

    public static Structures.Branch[] GetRelatedBranches(Structures.Commit commit)
    {
        var repository = GetRepositoryFrom(commit);
        return repository.Branches.Values.
            Collect(branch => branch.Head.Equals(commit) ? branch : null).
            ToArray();
    }

    public static Structures.Tag[] GetRelatedTags(Structures.Commit commit)
    {
        var repository = GetRepositoryFrom(commit);
        return repository.Tags.Values.
            Collect(tag =>
                (tag.Type == ObjectTypes.Commit &&
                 tag.Hash.Equals(commit.Hash)) ? tag : null).
            ToArray();
    }
}
