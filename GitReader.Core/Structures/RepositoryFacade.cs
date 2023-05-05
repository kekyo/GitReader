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
using System.Diagnostics;
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

    //////////////////////////////////////////////////////////////////////////

    private static async Task<ReadOnlyDictionary<string, Branch>> GetStructuredBranchesAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        Debug.Assert(object.ReferenceEquals(rwr.Target, repository));

        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, ReferenceTypes.Branches, ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await RepositoryAccessor.ReadCommitAsync(
                    repository, reference.Target, ct)
            }));

        return entries.
            Where(entry => entry.Head.HasValue).
            ToDictionary(
                entry => entry.Name,
                entry => new Branch(
                    entry.Name,
                    new Commit(rwr, entry.Head!.Value)));
    }

    private static async Task<ReadOnlyDictionary<string, Branch>> GetStructuredRemoteBranchesAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        Debug.Assert(object.ReferenceEquals(rwr.Target, repository));

        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, ReferenceTypes.RemoteBranches, ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await RepositoryAccessor.ReadCommitAsync(
                    repository, reference.Target, ct)
            }));

        return entries.
            Where(entry => entry.Head.HasValue).
            ToDictionary(
                entry => entry.Name,
                entry => new Branch(
                    entry.Name,
                    new Commit(rwr, entry.Head!.Value)));
    }

    private static async Task<ReadOnlyDictionary<string, Tag>> GetStructuredTagsAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        var references = await RepositoryAccessor.ReadReferencesAsync(
            repository, ReferenceTypes.Tags, ct);
        var tags = await Utilities.WhenAll(
            references.Select(async reference =>
            {
                // Tag object is read.
                if (await RepositoryAccessor.ReadTagAsync(
                    repository, reference.Target, ct) is { } tag)
                {
                    // TODO: Currently does not support any other tag types.
                    if (tag.Type == ObjectTypes.Commit)
                    {
                        // Target commit object is read.
                        if (await RepositoryAccessor.ReadCommitAsync(
                            repository, tag.Hash, ct) is { } commit)
                        {
                            return (Tag)new CommitTag(
                                tag.Hash, tag.Name, tag.Tagger, tag.Message,
                                new(rwr, commit));
                        }
                    }
                }
                else
                {
                    // Target commit object is read.
                    if (await RepositoryAccessor.ReadCommitAsync(
                        repository, reference.Target, ct) is { } commit)
                    {
                        return (Tag)new CommitTag(
                            reference.Target, reference.Name, null, null,
                            new(rwr, commit));
                    }
                }
                return null;
            }));

        return tags.
            Where(tag => tag != null).
            DistinctBy(tag => tag!.Name).
            ToDictionary(tag => tag!.Name, tag => tag!);
    }

    //////////////////////////////////////////////////////////////////////////

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
            // Read remote references from config file.
            repository.remoteReferenceUrlCache =
                await RepositoryAccessor.ReadRemoteReferencesAsync(repository, ct);

            // Read FETCH_HEAD and packed-refs.
            var (fhc1, fhc2) = await Utilities.Join(
                RepositoryAccessor.ReadFetchHeadsAsync(repository, ct),
                RepositoryAccessor.ReadPackedRefsAsync(repository, ct));
            repository.referenceCache = fhc1.Combine(fhc2);

            // Read all other requirements.
            var rwr = new WeakReference(repository);
            var (head, branches, remoteBranches, tags) = await Utilities.Join(
                GetCurrentHeadAsync(repository, ct),
                GetStructuredBranchesAsync(repository, rwr, ct),
                GetStructuredRemoteBranchesAsync(repository, rwr, ct),
                GetStructuredTagsAsync(repository, rwr, ct));

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

    //////////////////////////////////////////////////////////////////////////

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
            repository.objectAccessor == null)
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
                (tag is CommitTag &&
                 tag.Hash.Equals(commit.Hash)) ? tag : null).
            ToArray();
    }
}
