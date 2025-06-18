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

public static class RepositoryExtension
{
    public static Task<Commit?> GetCommitAsync(
        this StructuredRepository repository,
        Hash commit, CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitDirectlyAsync(repository, commit, ct);

    public static Task<Commit> GetHeadCommitAsync(
        this Branch branch,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(branch, ct);

    public static Task<Commit> GetCommitAsync(
        this Tag tag,
        CancellationToken ct = default) =>
        tag.Type switch
        {
            ObjectTypes.Commit => StructuredRepositoryFacade.GetCommitAsync(tag, ct),
            _ => throw new InvalidOperationException($"Could not get commit: Type={tag.Type}"),
        };

    public static Task<Annotation> GetAnnotationAsync(
        this Tag tag,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetAnnotationAsync(tag, ct);

    public static Task<Commit> GetCommitAsync(
        this Stash stash,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(stash, ct);

    public static Task<Commit> GetCurrentCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(reflog, reflog.Commit, ct);

    public static Task<Commit> GetOldCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetCommitAsync(reflog, reflog.OldCommit, ct);

    public static Task<Commit?> GetPrimaryParentCommitAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetPrimaryParentAsync(commit, ct);

    public static Task<Commit[]> GetParentCommitsAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetParentsAsync(commit, ct);

    public static Task<TreeRoot> GetTreeRootAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetTreeAsync(commit, ct);

    public static Task<Stream> OpenBlobAsync(
        this TreeBlobEntry entry,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenBlobAsync(entry, ct);

    public static Task<StructuredRepository> OpenSubModuleAsync(
        this TreeSubModuleEntry subModule,
        CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenSubModuleAsync(subModule, ct);

    public static string GetMessage(
        this Commit commit) =>
        commit.message;
    
    public static Task<ReflogEntry[]> GetHeadReflogsAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
        StructuredRepositoryFacade.GetHeadReflogsAsync(
            repository, new WeakReference(repository), ct);

    /// <summary>
    /// Gets working directory status with optional file path filtering.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A Task containing the structured working directory status.</returns>
    public static Task<WorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusAsync(
            repository, new WeakReference(repository), ct).AsTask();
#else
        WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusAsync(
            repository, new WeakReference(repository), ct);
#endif

    /// <summary>
    /// Gets working directory status with optional file path filtering.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="pathFilter">An optional predicate to filter files by path. If null, all files are included.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A Task containing the structured working directory status.</returns>
    public static Task<WorkingDirectoryStatus> GetWorkingDirectoryStatusWithFilterAsync(
        this StructuredRepository repository,
        Func<string, bool> pathFilter,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
        WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusWithFilterAsync(
            repository, new WeakReference(repository), pathFilter, ct).AsTask();
#else
        WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusWithFilterAsync(
            repository, new WeakReference(repository), pathFilter, ct);
#endif

    public static async Task<ReadOnlyArray<Worktree>> GetWorktreesAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
        await WorktreeAccessor.GetStructuredWorktreesAsync(
            repository, new WeakReference(repository), ct);

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

    public static void Deconstruct(
        this Branch branch,
        out string name,
        out Hash head)
    {
        name = branch.Name;
        head = branch.Head;
    }

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

    public static void Deconstruct(
        this Annotation annotation,
        out Signature? tagger,
        out string? message)
    {
        tagger = annotation.Tagger;
        message = annotation.Message;
    }

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
