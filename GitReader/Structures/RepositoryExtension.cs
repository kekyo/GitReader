////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using GitReader.Structures;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

/// <summary>
/// Provides extension methods for structured repository operations.
/// </summary>
public static class RepositoryExtension
{
    /// <summary>
    /// Gets a commit from the repository by its hash.
    /// </summary>
    /// <param name="repository">The structured repository.</param>
    /// <param name="commit">The commit hash to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the commit, or null if not found.</returns>
    public static Task<Commit?> GetCommitAsync(
        this StructuredRepository repository,
        Hash commit, CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitDirectlyAsync(repository, commit, ct);

    /// <summary>
    /// Gets the head commit of the specified branch.
    /// </summary>
    /// <param name="branch">The branch to get the head commit from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the head commit of the branch.</returns>
    public static Task<Commit> GetHeadCommitAsync(
        this Branch branch,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(branch, ct);

    /// <summary>
    /// Gets the commit that the specified tag points to.
    /// </summary>
    /// <param name="tag">The tag to get the commit from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the commit that the tag points to.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tag does not point to a commit.</exception>
    public static Task<Commit> GetCommitAsync(
        this Tag tag,
        CancellationToken ct = default) =>
        tag.Type switch
        {
            ObjectTypes.Commit => StructuredRepositoryFacade.GetCommitAsync(tag, ct),
            _ => throw new InvalidOperationException($"Could not get commit: Type={tag.Type}"),
        };

    /// <summary>
    /// Gets the annotation associated with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to get the annotation from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the annotation of the tag.</returns>
    public static Task<Annotation> GetAnnotationAsync(
        this Tag tag,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetAnnotationAsync(tag, ct);

    /// <summary>
    /// Gets the commit associated with the specified stash entry.
    /// </summary>
    /// <param name="stash">The stash entry to get the commit from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the commit associated with the stash.</returns>
    public static Task<Commit> GetCommitAsync(
        this Stash stash,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(stash, ct);

    /// <summary>
    /// Gets the current commit from the specified reflog entry.
    /// </summary>
    /// <param name="reflog">The reflog entry.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the current commit of the reflog entry.</returns>
    public static Task<Commit> GetCurrentCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(reflog, reflog.Commit, ct);

    /// <summary>
    /// Gets the old commit from the specified reflog entry.
    /// </summary>
    /// <param name="reflog">The reflog entry.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the old commit of the reflog entry.</returns>
    public static Task<Commit> GetOldCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(reflog, reflog.OldCommit, ct);

    /// <summary>
    /// Gets the primary parent commit of the specified commit.
    /// </summary>
    /// <param name="commit">The commit to get the primary parent from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the primary parent commit, or null if it has no parents.</returns>
    public static Task<Commit?> GetPrimaryParentCommitAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetPrimaryParentAsync(commit, ct);

    /// <summary>
    /// Gets all parent commits of the specified commit.
    /// </summary>
    /// <param name="commit">The commit to get the parents from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of parent commits.</returns>
    public static Task<Commit[]> GetParentCommitsAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetParentsAsync(commit, ct);

    /// <summary>
    /// Gets the tree root associated with the specified commit.
    /// </summary>
    /// <param name="commit">The commit to get the tree root from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the tree root of the commit.</returns>
    public static Task<TreeRoot> GetTreeRootAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetTreeAsync(commit, ct);

    /// <summary>
    /// Opens a blob stream for the specified tree blob entry.
    /// </summary>
    /// <param name="entry">The tree blob entry to open.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a stream for reading the blob content.</returns>
    public static Task<Stream> OpenBlobAsync(
        this TreeBlobEntry entry,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenBlobAsync(entry, ct);

    /// <summary>
    /// Opens the submodule repository associated with the specified tree submodule entry.
    /// </summary>
    /// <param name="subModule">The tree submodule entry.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the structured repository of the submodule.</returns>
    public static Task<StructuredRepository> OpenSubModuleAsync(
        this TreeSubModuleEntry subModule,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenSubModuleAsync(subModule, ct);

    /// <summary>
    /// Gets the full commit message of the specified commit.
    /// </summary>
    /// <param name="commit">The commit to get the message from.</param>
    /// <returns>The full commit message.</returns>
    public static string GetMessage(
        this Commit commit) =>
        commit.message;
    
    /// <summary>
    /// Gets the head reflog entries for the repository.
    /// </summary>
    /// <param name="repository">The structured repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of reflog entries.</returns>
    public static Task<ReflogEntry[]> GetHeadReflogsAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetHeadReflogsAsync(
            repository, new WeakReference(repository), ct);

    /// <summary>
    /// Gets working directory status.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A Task containing the structured working directory status.</returns>
    public static Task<WorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        this StructuredRepository repository,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
            repository, Internal.Glob.nothingFilter, ct).AsTask();
#else
        StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
            repository, Internal.Glob.nothingFilter, ct);
#endif

    /// <summary>
    /// Gets working directory status with an override glob filter.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="overrideGlobFilter">A predicate override glob filter function.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A Task containing the structured working directory status.</returns>
    public static Task<WorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        this StructuredRepository repository,
        GlobFilter overrideGlobFilter,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
            repository, overrideGlobFilter, ct).AsTask();
#else
        StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
            repository, overrideGlobFilter, ct);
#endif

    /// <summary>
    /// Gets all worktrees associated with the repository.
    /// </summary>
    /// <param name="repository">The structured repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a read-only array of worktrees.</returns>
    public static Task<ReadOnlyArray<Worktree>> GetWorktreesAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        WorktreeAccessor.GetStructuredWorktreesAsync(repository, ct).AsTask();
#else
        WorktreeAccessor.GetStructuredWorktreesAsync(repository, ct);
#endif

    /// <summary>
    /// Deconstructs a StructuredRepository into its component parts (including all branches).
    /// </summary>
    /// <param name="repository">The repository to deconstruct.</param>
    /// <param name="gitPath">The Git repository path.</param>
    /// <param name="remoteUrls">The remote URLs configured for the repository.</param>
    /// <param name="head">The current head branch, or null if in detached HEAD state.</param>
    /// <param name="branchesAll">All branches grouped by name (including remote branches).</param>
    /// <param name="tags">All tags in the repository.</param>
    public static void Deconstruct(
        this StructuredRepository repository,
        out string gitPath,
        out ReadOnlyDictionary<string, string> remoteUrls,
        out Branch? head,
        out ReadOnlyDictionary<string, Branch[]> branchesAll,
        out ReadOnlyDictionary<string, Tag> tags)
    {
        gitPath = repository.GitPath;
        remoteUrls = repository.RemoteUrls;
        head = repository.head;
        branchesAll = repository.branchesAll;
        tags = repository.tags;
    }

    /// <summary>
    /// Deconstructs a StructuredRepository into its component parts (primary branches only).
    /// </summary>
    /// <param name="repository">The repository to deconstruct.</param>
    /// <param name="gitPath">The Git repository path.</param>
    /// <param name="remoteUrls">The remote URLs configured for the repository.</param>
    /// <param name="head">The current head branch, or null if in detached HEAD state.</param>
    /// <param name="branches">The primary branches in the repository.</param>
    /// <param name="tags">All tags in the repository.</param>
    public static void Deconstruct(
        this StructuredRepository repository,
        out string gitPath,
        out ReadOnlyDictionary<string, string> remoteUrls,
        out Branch? head,
        out ReadOnlyDictionary<string, Branch> branches,
        out ReadOnlyDictionary<string, Tag> tags)
    {
        gitPath = repository.GitPath;
        remoteUrls = repository.RemoteUrls;
        head = repository.head;
        branches = repository.Branches;
        tags = repository.Tags;
    }

    /// <summary>
    /// Deconstructs a Branch into its component parts.
    /// </summary>
    /// <param name="branch">The branch to deconstruct.</param>
    /// <param name="name">The name of the branch.</param>
    /// <param name="head">The head commit hash of the branch.</param>
    public static void Deconstruct(
        this Branch branch,
        out string name,
        out Hash head)
    {
        name = branch.Name;
        head = branch.Head;
    }

    /// <summary>
    /// Deconstructs a Commit into its component parts (with separate subject and body).
    /// </summary>
    /// <param name="commit">The commit to deconstruct.</param>
    /// <param name="hash">The commit hash.</param>
    /// <param name="author">The author signature.</param>
    /// <param name="committer">The committer signature.</param>
    /// <param name="subject">The commit subject (first line of the message).</param>
    /// <param name="body">The commit body (remaining lines of the message).</param>
    /// <remarks>
    /// The subject line is the first line of the commit message. It is nearly as git command `git log --format=%s`.
    /// The body is the rest of the commit message after the first blank line. It is nearly as git command `git log --format=%b`.
    /// </remarks>
    public static void Deconstruct(
        this Commit commit,
        out Hash hash,
        out Signature author,
        out Signature committer,
        out string subject,
        out string body)
    {
        hash = commit.Hash;
        author = commit.Author;
        committer = commit.Committer;
        subject = commit.Subject;
        body = commit.Body;
    }

    /// <summary>
    /// Deconstructs a Commit into its component parts (with full message).
    /// </summary>
    /// <param name="commit">The commit to deconstruct.</param>
    /// <param name="hash">The commit hash.</param>
    /// <param name="author">The author signature.</param>
    /// <param name="committer">The committer signature.</param>
    /// <param name="message">The full commit message.</param>
    public static void Deconstruct(
        this Commit commit,
        out Hash hash,
        out Signature author,
        out Signature committer,
        out string message)
    {
        hash = commit.Hash;
        author = commit.Author;
        committer = commit.Committer;
        message = commit.message;
    }

    /// <summary>
    /// Deconstructs a Tag into its component parts.
    /// </summary>
    /// <param name="tag">The tag to deconstruct.</param>
    /// <param name="tagHash">The tag hash.</param>
    /// <param name="type">The type of the tag.</param>
    /// <param name="objectHash">The hash of the object the tag points to.</param>
    /// <param name="name">The name of the tag.</param>
    public static void Deconstruct(
        this Tag tag,
        out Hash? tagHash,
        out ObjectTypes type,
        out Hash objectHash,
        out string name)
    {
        tagHash = tag.TagHash;
        type = tag.Type;
        objectHash = tag.ObjectHash;
        name = tag.Name;
    }

    /// <summary>
    /// Deconstructs an Annotation into its component parts.
    /// </summary>
    /// <param name="annotation">The annotation to deconstruct.</param>
    /// <param name="tagger">The tagger signature.</param>
    /// <param name="message">The annotation message.</param>
    public static void Deconstruct(
        this Annotation annotation,
        out Signature? tagger,
        out string? message)
    {
        tagger = annotation.Tagger;
        message = annotation.Message;
    }

    /// <summary>
    /// Deconstructs a WorkingDirectoryFile into its component parts.
    /// </summary>
    /// <param name="file">The working directory file to deconstruct.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="status">The status of the file.</param>
    /// <param name="indexHash">The hash of the file in the index.</param>
    /// <param name="workingTreeHash">The hash of the file in the working tree.</param>
    public static void Deconstruct(
        this WorkingDirectoryFile file,
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
    /// Deconstructs a WorkingDirectoryStatus into its component parts.
    /// </summary>
    /// <param name="status">The working directory status to deconstruct.</param>
    /// <param name="stagedFiles">The staged files.</param>
    /// <param name="unstagedFiles">The unstaged files.</param>
    /// <param name="untrackedFiles">The untracked files.</param>
    public static void Deconstruct(
        this WorkingDirectoryStatus status,
        out ReadOnlyArray<WorkingDirectoryFile> stagedFiles,
        out ReadOnlyArray<WorkingDirectoryFile> unstagedFiles,
        out ReadOnlyArray<WorkingDirectoryFile> untrackedFiles)
    {
        stagedFiles = status.StagedFiles;
        unstagedFiles = status.UnstagedFiles;
        untrackedFiles = status.UntrackedFiles;
    }

    /// <summary>
    /// Deconstructs a Worktree into its component parts.
    /// </summary>
    /// <param name="worktree">The worktree to deconstruct.</param>
    /// <param name="name">The name of the worktree.</param>
    /// <param name="path">The path of the worktree.</param>
    /// <param name="head">The head commit hash of the worktree.</param>
    /// <param name="branch">The branch of the worktree.</param>
    /// <param name="status">The status of the worktree.</param>
    public static void Deconstruct(
        this Worktree worktree,
        out string name,
        out string path,
        out Hash? head,
        out string? branch,
        out WorktreeStatus status)
    {
        name = worktree.Name;
        path = worktree.Path;
        head = worktree.Head;
        branch = worktree.Branch;
        status = worktree.Status;
    }
}
