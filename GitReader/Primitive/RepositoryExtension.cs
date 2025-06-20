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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

/// <summary>
/// Provides extension methods for primitive repository operations.
/// </summary>
public static class RepositoryExtension
{
    /// <summary>
    /// Gets the current head reference from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the current head reference, or null if not found.</returns>
    public static Task<PrimitiveReference?> GetCurrentHeadReferenceAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(repository, ct);

    /// <summary>
    /// Gets the head reference for the specified branch.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the branch head reference.</returns>
    public static Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        this PrimitiveRepository repository,
        string branchName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetBranchHeadReferenceAsync(repository, branchName, ct);

    /// <summary>
    /// Gets all head references for the specified branch (including remote branches).
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of all head references for the branch.</returns>
    public static Task<PrimitiveReference[]> GetBranchAllHeadReferenceAsync(
        this PrimitiveRepository repository,
        string branchName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetBranchAllHeadReferenceAsync(repository, branchName, ct);

    /// <summary>
    /// Gets the tag reference for the specified tag name.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="tagName">The name of the tag.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the tag reference.</returns>
    public static Task<PrimitiveTagReference> GetTagReferenceAsync(
        this PrimitiveRepository repository,
        string tagName, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.GetTagReferenceAsync(repository, tagName, ct);

    /// <summary>
    /// Gets a commit by its hash from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="commit">The commit hash to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the commit, or null if not found.</returns>
    public static Task<PrimitiveCommit?> GetCommitAsync(
        this PrimitiveRepository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryAccessor.ReadCommitAsync(repository, commit, ct);

    /// <summary>
    /// Gets a tag from the specified tag reference.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="tag">The tag reference.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the primitive tag.</returns>
    public static async Task<PrimitiveTag> GetTagAsync(
        this PrimitiveRepository repository,
        PrimitiveTagReference tag, CancellationToken ct = default) =>
        await RepositoryAccessor.ReadTagAsync(repository, tag.ObjectOrCommitHash, ct) is { } t ?
            t : new(tag.ObjectOrCommitHash, ObjectTypes.Commit, tag.Name, null, null);

    /// <summary>
    /// Gets all branch head references from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of branch head references.</returns>
    public static Task<PrimitiveReference[]> GetBranchHeadReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.Branches, ct);

    /// <summary>
    /// Gets all remote branch head references from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of remote branch head references.</returns>
    public static Task<PrimitiveReference[]> GetRemoteBranchHeadReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.RemoteBranches, ct);

    /// <summary>
    /// Gets all tag references from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of tag references.</returns>
    public static Task<PrimitiveTagReference[]> GetTagReferencesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadTagReferencesAsync(repository, ct);
    
    /// <summary>
    /// Gets all stashes from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of stash reflog entries.</returns>
    public static Task<PrimitiveReflogEntry[]> GetStashesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadStashesAsync(repository, ct);
    
    /// <summary>
    /// Gets reflog entries related to the specified reference.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="reference">The reference to get reflog entries for.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of reflog entries.</returns>
    public static Task<PrimitiveReflogEntry[]> GetRelatedReflogsAsync(
        this PrimitiveRepository repository,
        PrimitiveReference reference,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReflogEntriesAsync(repository, reference, ct);

    /// <summary>
    /// Gets a tree by its hash from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="tree">The tree hash to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the primitive tree.</returns>
    public static Task<PrimitiveTree> GetTreeAsync(
        this PrimitiveRepository repository,
        Hash tree,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadTreeAsync(repository, tree, ct);

    /// <summary>
    /// Opens a blob stream by its hash from the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="blob">The blob hash to open.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a stream for reading the blob content.</returns>
    public static Task<Stream> OpenBlobAsync(
        this PrimitiveRepository repository,
        Hash blob,
        CancellationToken ct = default) =>
        RepositoryAccessor.OpenBlobAsync(repository, blob, ct);

    /// <summary>
    /// Opens a submodule repository from the specified tree path.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="treePath">The tree path to the submodule.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the submodule repository.</returns>
    public static Task<PrimitiveRepository> OpenSubModuleAsync(
        this PrimitiveRepository repository,
        PrimitiveTreeEntry[] treePath,
        CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.OpenSubModuleAsync(repository, treePath, ct);

    /// <summary>
    /// Gets working directory status with optional file path filtering.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A Task containing the primitive working directory status.</returns>
    public static Task<PrimitiveWorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        PrimitiveRepositoryFacade.GetWorkingDirectoryStatusAsync(repository, ct).AsTask();
#else
        PrimitiveRepositoryFacade.GetWorkingDirectoryStatusAsync(repository, ct);
#endif

    public static Task<ReadOnlyArray<PrimitiveWorkingDirectoryFile>> GetUntrackedFilesAsync(
        this PrimitiveRepository repository,
        PrimitiveWorkingDirectoryStatus workingDirectoryStatus,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
            repository, workingDirectoryStatus, Internal.Glob.nothingFilter, ct).AsTask();
#else
        PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
            repository, workingDirectoryStatus, Internal.Glob.nothingFilter, ct);
#endif

    public static Task<ReadOnlyArray<PrimitiveWorkingDirectoryFile>> GetUntrackedFilesAsync(
        this PrimitiveRepository repository,
        PrimitiveWorkingDirectoryStatus workingDirectoryStatus,
        GlobFilter overrideGlobFilter,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
            repository, workingDirectoryStatus, overrideGlobFilter, ct).AsTask();
#else
        PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
            repository, workingDirectoryStatus, overrideGlobFilter, ct);
#endif

    /// <summary>
    /// Gets all worktrees associated with the repository.
    /// </summary>
    /// <param name="repository">The primitive repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a read-only array of worktrees.</returns>
    public static Task<ReadOnlyArray<PrimitiveWorktree>> GetWorktreesAsync(
        this PrimitiveRepository repository,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        WorktreeAccessor.GetPrimitiveWorktreesAsync(repository, ct).AsTask();
#else
        WorktreeAccessor.GetPrimitiveWorktreesAsync(repository, ct);
#endif

    /// <summary>
    /// Gets the head commit hash for the specified worktree.
    /// </summary>
    /// <param name="worktree">The primitive worktree.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the head commit hash, or null if not available.</returns>
    public static Task<Hash?> GetHeadAsync(
        this PrimitiveWorktree worktree,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        WorktreeAccessor.GetWorktreeHeadAsync(worktree, ct).AsTask();
#else
        WorktreeAccessor.GetWorktreeHeadAsync(worktree, ct);
#endif

    /// <summary>
    /// Gets the branch name for the specified worktree.
    /// </summary>
    /// <param name="worktree">The primitive worktree.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the branch name, or null if not available.</returns>
    public static Task<string?> GetBranchAsync(
        this PrimitiveWorktree worktree,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        WorktreeAccessor.GetWorktreeBranchAsync(worktree, ct).AsTask();
#else
        WorktreeAccessor.GetWorktreeBranchAsync(worktree, ct);
#endif

    /// <summary>
    /// Deconstructs a PrimitiveRepository into its component parts.
    /// </summary>
    /// <param name="repository">The repository to deconstruct.</param>
    /// <param name="gitPath">The Git repository path.</param>
    /// <param name="remoteUrls">The remote URLs configured for the repository.</param>
    public static void Deconstruct(
        this PrimitiveRepository repository,
        out string gitPath,
        out ReadOnlyDictionary<string, string> remoteUrls)
    {
        gitPath = repository.GitPath;
        remoteUrls = repository.RemoteUrls;
    }

    /// <summary>
    /// Deconstructs a PrimitiveReference into its component parts.
    /// </summary>
    /// <param name="reference">The reference to deconstruct.</param>
    /// <param name="name">The name of the reference.</param>
    /// <param name="relativePath">The relative path of the reference.</param>
    /// <param name="target">The target hash of the reference.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveTagReference into its component parts.
    /// </summary>
    /// <param name="tagReference">The tag reference to deconstruct.</param>
    /// <param name="name">The name of the tag reference.</param>
    /// <param name="relativePath">The relative path of the tag reference.</param>
    /// <param name="objectOrCommitHash">The object or commit hash.</param>
    /// <param name="commitHash">The commit hash if this is an annotated tag.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveCommit into its component parts.
    /// </summary>
    /// <param name="commit">The commit to deconstruct.</param>
    /// <param name="hash">The commit hash.</param>
    /// <param name="treeRoot">The tree root hash.</param>
    /// <param name="author">The author signature.</param>
    /// <param name="committer">The committer signature.</param>
    /// <param name="parents">The parent commit hashes.</param>
    /// <param name="message">The commit message.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveTag into its component parts.
    /// </summary>
    /// <param name="tag">The tag to deconstruct.</param>
    /// <param name="hash">The tag hash.</param>
    /// <param name="type">The object type of the tag.</param>
    /// <param name="name">The tag name.</param>
    /// <param name="tagger">The tagger signature.</param>
    /// <param name="message">The tag message.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveReflogEntry into its component parts.
    /// </summary>
    /// <param name="reflog">The reflog entry to deconstruct.</param>
    /// <param name="old">The old commit hash.</param>
    /// <param name="current">The current commit hash.</param>
    /// <param name="committer">The committer signature.</param>
    /// <param name="message">The reflog message.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveTree into its component parts.
    /// </summary>
    /// <param name="tree">The tree to deconstruct.</param>
    /// <param name="hash">The tree hash.</param>
    /// <param name="children">The child entries of the tree.</param>
    public static void Deconstruct(
        this PrimitiveTree tree,
        out Hash hash,
        out ReadOnlyArray<PrimitiveTreeEntry> children)
    {
        hash = tree.Hash;
        children = tree.Children;
    }

    /// <summary>
    /// Deconstructs a PrimitiveTreeEntry into its component parts.
    /// </summary>
    /// <param name="entry">The tree entry to deconstruct.</param>
    /// <param name="hash">The entry hash.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="modes">The entry mode flags.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveWorkingDirectoryFile into its component parts.
    /// </summary>
    /// <param name="file">The working directory file to deconstruct.</param>
    /// <param name="path">The file path.</param>
    /// <param name="status">The file status.</param>
    /// <param name="indexHash">The hash in the index, if any.</param>
    /// <param name="workingTreeHash">The hash in the working tree, if any.</param>
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

    /// <summary>
    /// Deconstructs a PrimitiveWorkingDirectoryStatus into its component parts.
    /// </summary>
    /// <param name="status">The working directory status to deconstruct.</param>
    /// <param name="stagedFiles">The files staged for commit.</param>
    /// <param name="unstagedFiles">The files with unstaged changes.</param>
    public static void Deconstruct(
        this PrimitiveWorkingDirectoryStatus status,
        out ReadOnlyArray<PrimitiveWorkingDirectoryFile> stagedFiles,
        out ReadOnlyArray<PrimitiveWorkingDirectoryFile> unstagedFiles)
    {
        stagedFiles = status.StagedFiles;
        unstagedFiles = status.UnstagedFiles;
    }

    /// <summary>
    /// Deconstructs a PrimitiveWorktree into its component parts.
    /// </summary>
    /// <param name="worktree">The worktree to deconstruct.</param>
    /// <param name="name">The worktree name.</param>
    /// <param name="path">The worktree path.</param>
    /// <param name="status">The worktree status.</param>
    public static void Deconstruct(
        this PrimitiveWorktree worktree,
        out string name,
        out string path,
        out WorktreeStatus status)
    {
        name = worktree.Name;
        path = worktree.Path;
        status = worktree.Status;
    }
}
