////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using GitReader.Collections;
using GitReader.Primitive;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

public static class RepositoryExtension
{
    public static Task<PrimitiveReference?> GetCurrentHeadReferenceAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(repository, ct);

    public static Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        this PrimitiveRepository repository,
        string branchName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetBranchHeadReferenceAsync(repository, branchName, ct);

    public static Task<PrimitiveReference[]> GetBranchAllHeadReferenceAsync(
        this PrimitiveRepository repository,
        string branchName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetBranchAllHeadReferenceAsync(repository, branchName, ct);

    public static Task<PrimitiveTagReference> GetTagReferenceAsync(
        this PrimitiveRepository repository,
        string tagName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetTagReferenceAsync(repository, tagName, ct);

    public static Task<PrimitiveCommit?> GetCommitAsync(
        this PrimitiveRepository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryAccessor.ReadCommitAsync(repository, commit, ct);

    public static async Task<PrimitiveTag> GetTagAsync(
        this PrimitiveRepository repository,
        PrimitiveTagReference tag, CancellationToken ct = default) =>
        await RepositoryAccessor.ReadTagAsync(repository, tag.ObjectOrCommitHash, ct) is { } t ?
            t : new(tag.ObjectOrCommitHash, ObjectTypes.Commit, tag.Name, null, null);

    public static Task<PrimitiveReference[]> GetBranchHeadReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.Branches, ct);

    public static Task<PrimitiveReference[]> GetRemoteBranchHeadReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.RemoteBranches, ct);

    public static Task<PrimitiveTagReference[]> GetTagReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadTagReferencesAsync(repository, ct);
    
    public static Task<PrimitiveReflogEntry[]> GetStashesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadStashesAsync(repository, ct);
    
    public static Task<PrimitiveReflogEntry[]> GetRelatedReflogsAsync(
        this PrimitiveRepository repository,
        PrimitiveReference reference,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReflogEntriesAsync(repository, reference, ct);

    public static Task<PrimitiveTree> GetTreeAsync(
        this PrimitiveRepository repository,
        Hash tree,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadTreeAsync(repository, tree, ct);

    public static Task<Stream> OpenBlobAsync(
        this PrimitiveRepository repository,
        Hash blob,
        CancellationToken ct = default) =>
        RepositoryAccessor.OpenBlobAsync(repository, blob, ct);

    public static Task<PrimitiveRepository> OpenSubModuleAsync(
        this PrimitiveRepository repository,
        PrimitiveTreeEntry[] treePath,
        CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.OpenSubModuleAsync(repository, treePath, ct);

    public static async Task<PrimitiveWorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        await WorkingDirectoryAccessor.GetPrimitiveWorkingDirectoryStatusAsync(repository, ct);

    public static void Deconstruct(
        this PrimitiveRepository repository,
        out string gitPath,
        out ReadOnlyDictionary<string, string> remoteUrls)
    {
        gitPath = repository.GitPath;
        remoteUrls = repository.RemoteUrls;
    }

    public static void Deconstruct(
        this PrimitiveReference reference,
        out string name,
        out string relativePath,
        out Hash target)
    {
        name = reference.Name;
        relativePath = reference.RelativePath;
        target = reference.Target;
    }

    public static void Deconstruct(
        this PrimitiveTagReference tagReference,
        out string name,
        out string relativePath,
        out Hash objectOrCommitHash,
        out Hash? commitHash)
    {
        name = tagReference.Name;
        relativePath = tagReference.RelativePath;
        objectOrCommitHash = tagReference.ObjectOrCommitHash;
        commitHash = tagReference.CommitHash;
    }

    public static void Deconstruct(
        this PrimitiveCommit commit,
        out Hash hash,
        out Hash treeRoot,
        out Signature author,
        out Signature committer,
        out ReadOnlyArray<Hash> parents,
        out string message)
    {
        hash = commit.Hash;
        treeRoot = commit.TreeRoot;
        author = commit.Author;
        committer = commit.Committer;
        parents = commit.Parents;
        message = commit.Message;
    }

    public static void Deconstruct(
        this PrimitiveTag tag,
        out Hash hash,
        out ObjectTypes type,
        out string name,
        out Signature? tagger,
        out string? message)
    {
        hash = tag.Hash;
        type = tag.Type;
        name = tag.Name;
        tagger = tag.Tagger;
        message = tag.Message;
    }

    public static void Deconstruct(
        this PrimitiveReflogEntry reflog,
        out Hash old,
        out Hash current,
        out Signature committer,
        out string message)
    {
        old = reflog.Old;
        current = reflog.Current;
        committer = reflog.Committer;
        message = reflog.Message;
    }

    public static void Deconstruct(
        this PrimitiveTree tree,
        out Hash hash,
        out ReadOnlyArray<PrimitiveTreeEntry> children)
    {
        hash = tree.Hash;
        children = tree.Children;
    }

    public static void Deconstruct(
        this PrimitiveTreeEntry entry,
        out Hash hash,
        out string name,
        out PrimitiveModeFlags modes)
    {
        hash = entry.Hash;
        name = entry.Name;
        modes = entry.Modes;
    }

    public static void Deconstruct(
        this PrimitiveWorkingDirectoryFile file,
        out string path,
        out FileStatus status,
        out Hash? indexHash,
        out Hash? workingTreeHash)
    {
        path = file.Path;
        status = file.Status;
        indexHash = file.IndexHash;
        workingTreeHash = file.WorkingTreeHash;
    }

    public static void Deconstruct(
        this PrimitiveWorkingDirectoryStatus status,
        out ReadOnlyArray<PrimitiveWorkingDirectoryFile> stagedFiles,
        out ReadOnlyArray<PrimitiveWorkingDirectoryFile> unstagedFiles,
        out ReadOnlyArray<PrimitiveWorkingDirectoryFile> untrackedFiles)
    {
        stagedFiles = status.StagedFiles;
        unstagedFiles = status.UnstagedFiles;
        untrackedFiles = status.UntrackedFiles;
    }
}
