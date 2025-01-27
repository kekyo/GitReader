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
using GitReader.IO;
using GitReader.Primitive;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

internal static class StructuredRepositoryFacade
{
    public readonly struct RepositoryReferenceExtracted
    {
        public readonly StructuredRepository Repository;
        public readonly WeakReference WeakReference;

        public RepositoryReferenceExtracted(
            StructuredRepository repository, WeakReference weakReference)
        {
            this.Repository = repository;
            this.WeakReference = weakReference;
        }

        public void Deconstruct(out StructuredRepository repository, out WeakReference weakReference)
        {
            repository = this.Repository;
            weakReference = this.WeakReference;
        }
    }

    public static RepositoryReferenceExtracted GetRelatedRepository(
        this IRepositoryReference repositoryReference)
    {
        if (repositoryReference.Repository.Target is not StructuredRepository repository ||
            repository.objectAccessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }
        return new(repository, repositoryReference.Repository);
    }

    //////////////////////////////////////////////////////////////////////////

    private static async Task<Branch?> GetCurrentHeadAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        if (await RepositoryAccessor.ReadHashAsync(
            repository, "HEAD", ct) is { } results)
        {
            return new(rwr, results.Names.Last(), results.Hash, false);
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

        var (references, remoteReferences) = await Utilities.Join(
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Branches, ct),
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.RemoteBranches, ct));

        return references.
            Select(r => new Branch(rwr, r.Name, r.Target, false)).
            Concat(remoteReferences.Select(r => new Branch(rwr, r.Name, r.Target, true))).
            ToDictionary(b => b.Name);
    }

    private static async Task<ReadOnlyDictionary<string, Tag>> GetStructuredTagsAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        var tagReferences = await RepositoryAccessor.ReadTagReferencesAsync(
            repository, ct);

        var tags = await Utilities.WhenAll(
            tagReferences.Select(async tagReference =>
            {
                // If produced a peeled-tag, we can get the commit hash with no additional costs.
                if (tagReference.CommitHash is { } commitTarget)
                {
                    var tagHash = tagReference.ObjectOrCommitHash;
                    return new Tag(rwr, tagHash,
                        ObjectTypes.Commit, commitTarget, tagReference.Name,
                        null);
                }
                // If peeled-tags are not provided by the 'packed-refs' file at open time,
                // a tag object will be read occur here. This is expensive and extends open time.
                // However, since the commit hash cannot be identified without reading the tag object
                // (given that this is a high-level interface), a compromise is made.
                else if (await RepositoryAccessor.ReadTagAsync(
                    repository, tagReference.ObjectOrCommitHash, ct) is { } tag)
                {
                    var tagHash = tagReference.ObjectOrCommitHash;
                    return new Tag(rwr, tagHash,
                        tag.Type, tag.Hash, tagReference.Name,
                        new(tag.Tagger, tag.Message));
                }
                // If the read result shows that it is not a tag object, it is a commit object.
                else
                {
                    var commitHash = tagReference.ObjectOrCommitHash;
                    return new Tag(rwr, null,
                        ObjectTypes.Commit, commitHash, tagReference.Name,
                        null);
                }
            }));

        return tags.
            Where(tag => tag != null).
            DistinctBy(tag => tag!.Name).
            ToDictionary(tag => tag!.Name, tag => tag!);
    }

    private static async Task<Stash[]> GetStructuredStashesAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        var primitiveStashes = await RepositoryAccessor.ReadStashesAsync(repository, ct);
        
        return primitiveStashes.Select(stash =>
            new Stash(rwr, stash.Current, stash.Committer, stash.Message)).
            Reverse().
            ToArray();
    }
    
    public static async Task<ReflogEntry[]> GetHeadReflogsAsync(
        StructuredRepository repository,
        WeakReference rwr,
        CancellationToken ct)
    {
        var primitiveReflogEntries = await RepositoryAccessor.ReadReflogEntriesAsync(repository, "HEAD", ct);

        return primitiveReflogEntries.Select(stash =>
            new ReflogEntry(rwr, stash.Current, stash.Old, stash.Committer, stash.Message)).
            Reverse().
            ToArray();
    }

    //////////////////////////////////////////////////////////////////////////

    private static async Task<StructuredRepository> InternalOpenStructuredAsync(
        string repositoryPath,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var repository = new StructuredRepository(repositoryPath, fileSystem);

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

            // Read all other requirements.
            var rwr = new WeakReference(repository);
            var (head, branches, tags, stashes) = await Utilities.Join(
                GetCurrentHeadAsync(repository, rwr, ct),
                GetStructuredBranchesAsync(repository, rwr, ct),
                GetStructuredTagsAsync(repository, rwr, ct),
                GetStructuredStashesAsync(repository, rwr, ct));

            repository.head = head;
            repository.branches = branches;
            repository.tags = tags;
            repository.stashes = stashes;

            return repository;
        }
        catch
        {
            repository.Dispose();
            throw;
        }
    }

    public static async Task<StructuredRepository> OpenStructuredAsync(
        string path,
        IFileSystem fileSystem,
        CancellationToken ct)
    {
        var repositoryPath = await RepositoryAccessor.DetectLocalRepositoryPathAsync(
            path, fileSystem, ct);
        return await InternalOpenStructuredAsync(repositoryPath, fileSystem, ct);
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

    public static async Task<Commit> GetCommitAsync(
        IInternalCommitReference commitReference,
        CancellationToken ct)
    {
        var (repository, rwr) = GetRelatedRepository(commitReference);

        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, commitReference.Hash, ct);
        return new(rwr, commit!.Value);
    }

    public static async Task<Commit> GetCommitAsync(
        IRepositoryReference repositoryReference,
        Hash hash,
        CancellationToken ct)
    {
        var (repository, rwr) = GetRelatedRepository(repositoryReference);

        var commit = await RepositoryAccessor.ReadCommitAsync(
            repository, hash, ct);
        return new(rwr, commit!.Value);
    }

    public static async Task<Annotation> GetAnnotationAsync(
        Tag tag,
        CancellationToken ct)
    {
        if (tag.annotation is not { } annotation)
        {
            if (tag.TagHash is { } tagHash)
            {
                var (repository, _) = GetRelatedRepository(tag);

                var t = await RepositoryAccessor.ReadTagAsync(
                    repository, tagHash, ct);

                annotation = new(t!.Value.Tagger, t!.Value.Message);
                Interlocked.CompareExchange(ref tag.annotation, annotation, null);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Tag {tag.Name} does not have annotation.");
            }
        }
        return annotation;
    }

    public static async Task<Commit?> GetPrimaryParentAsync(
        Commit commit,
        CancellationToken ct)
    {
        if (commit.parents.Count == 0)
        {
            return null;
        }

        var (repository, rwr) = GetRelatedRepository(commit);

        var pc = await RepositoryAccessor.ReadCommitAsync(
            repository, commit.parents[0], ct);
        return pc is { } ?
            new(rwr, pc!.Value) :
            throw new InvalidDataException(
                $"Could not find a commit: {commit.parents[0]}");
    }

    public static Task<Commit[]> GetParentsAsync(
        Commit commit,
        CancellationToken ct)
    {
        var (repository, rwr) = GetRelatedRepository(commit);

        return Utilities.WhenAll(
            commit.parents.Select(async parent =>
            {
                var pc = await RepositoryAccessor.ReadCommitAsync(
                    repository, parent, ct);
                return pc is { } ?
                    new Commit(rwr, pc!.Value) :
                    throw new InvalidDataException(
                        $"Could not find a commit: {parent}");
            }));
    }

    public static Branch[] GetRelatedBranches(Commit commit)
    {
        var (repository, _) = GetRelatedRepository(commit);

        return repository.Branches.Values.
            Collect(branch => branch.Head.Equals(commit.Hash) ? branch : null).
            ToArray();
    }

    public static Tag[] GetRelatedTags(Commit commit)
    {
        var (repository, _) = GetRelatedRepository(commit);

        return repository.Tags.Values.
            Collect(tag => (tag.ObjectHash is { } oh && oh.Equals(commit.Hash)) ? tag : null).
            ToArray();
    }

    public static async Task<TreeRoot> GetTreeAsync(
        Commit commit,
        CancellationToken ct)
    {
        var (repository, rwr) = GetRelatedRepository(commit);

        var rootTree = await RepositoryAccessor.ReadTreeAsync(
            repository, commit.treeRoot, ct);

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        // This is a rather aggressive algorithm that recursively and in parallel searches all entries
        // in the tree and builds all elements.
        async ValueTask<TreeEntry[]> GetChildrenAsync(
            ReadOnlyArray<PrimitiveTreeEntry> entries, Tree parent) =>
            (await Utilities.WhenAll(
                entries.Select((Func<PrimitiveTreeEntry, ValueTask<TreeEntry>>)(async entry =>
                {
                    var modeFlags = (ModeFlags)((int)entry.Modes & 0x1ff);
                    switch (entry.SpecialModes)
                    {
                        case PrimitiveSpecialModes.Directory:
                            var tree = await RepositoryAccessor.ReadTreeAsync(
                                repository!, entry.Hash, ct);
                            var directory = new TreeDirectoryEntry(
                                entry.Hash, entry.Name, modeFlags, parent);
                            var children = await GetChildrenAsync(tree.Children, directory);
                            directory.SetChildren(children);
                            return directory;
                        case PrimitiveSpecialModes.Blob:
                            return new TreeBlobEntry(
                                rwr, entry.Hash, entry.Name, modeFlags, parent);
                        case PrimitiveSpecialModes.SubModule:
                            return new TreeSubModuleEntry(
                                rwr, entry.Hash, entry.Name, modeFlags, parent);
                        default:
                            // TODO:
                            return null!;
                    }
                })))).
            Where(entry => entry != null).
            ToArray();
#else
        // This is a rather aggressive algorithm that recursively and in parallel searches all entries
        // in the tree and builds all elements.
        async Task<TreeEntry[]> GetChildrenAsync(
            ReadOnlyArray<PrimitiveTreeEntry> entries, Tree parent) =>
            (await Utilities.WhenAll(
                entries.Select(async entry =>
                {
                    var modeFlags = (ModeFlags)((int)entry.Modes & 0x1ff);
                    switch (entry.SpecialModes)
                    {
                        case PrimitiveSpecialModes.Directory:
                            var tree = await RepositoryAccessor.ReadTreeAsync(
                                repository!, entry.Hash, ct);
                            var directory = new TreeDirectoryEntry(
                                entry.Hash, entry.Name, modeFlags, parent);
                            var children = await GetChildrenAsync(tree.Children, directory);
                            directory.SetChildren(children);
                            return (TreeEntry)directory;
                        case PrimitiveSpecialModes.Blob:
                            return new TreeBlobEntry(
                                rwr, entry.Hash, entry.Name, modeFlags, parent);
                        case PrimitiveSpecialModes.SubModule:
                            return new TreeSubModuleEntry(
                                rwr, entry.Hash, entry.Name, modeFlags, parent);
                        default:
                            // TODO:
                            return null!;
                    }
                }))).
            Where(entry => entry != null).
            ToArray();
#endif

        var treeRoot = new TreeRoot(commit.Hash);
        var children = await GetChildrenAsync(rootTree.Children, treeRoot);
        treeRoot.SetChildren(children);

        return treeRoot;
    }

    public static async Task<StructuredRepository> OpenSubModuleAsync(
        TreeSubModuleEntry subModule,
        CancellationToken ct)
    {
        var (repository, _) = GetRelatedRepository(subModule);

        var repositoryPath = repository.fileSystem.Combine(
            repository.GitPath,
            "modules",
            repository.fileSystem.Combine(subModule.
                Traverse<TreeEntry>(tree => tree.Parent as TreeEntry).
                Select(tree => tree.Name).
                Reverse().
                ToArray()));

        if (!await repository.fileSystem.IsFileExistsAsync(
            repository.fileSystem.Combine(repositoryPath, "config"), ct))
        {
            throw new ArgumentException("Submodule repository does not exist.");
        }

        return await InternalOpenStructuredAsync(repositoryPath, repository.fileSystem, ct);
    }

    public static Task<Stream> OpenBlobAsync(
        TreeBlobEntry entry,
        CancellationToken ct)
    {
        var (repository, rwr) = GetRelatedRepository(entry);

        return RepositoryAccessor.OpenBlobAsync(
            repository, entry.Hash, ct);
    }
}
