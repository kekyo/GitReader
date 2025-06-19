////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open GitReader.Internal
open GitReader.Structures
open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Provides F#-specific extension methods for structured repository operations.
/// </summary>
[<AutoOpen>]
module public RepositoryExtension =

    type StructuredRepository with
        /// <summary>
        /// Gets the current HEAD as an optional value.
        /// </summary>
        /// <returns>Some HEAD reference if available; otherwise, None.</returns>
        member repository.getCurrentHead() =
            match repository.Head with
            | null -> None
            | _ -> Some repository.Head

        /// <summary>
        /// Gets the commit object for the specified hash.
        /// </summary>
        /// <param name="commit">The hash of the commit.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an optional commit object.</returns>
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetCommitDirectlyAsync(
                repository, commit, unwrapCT ct).asAsync()
            return match c with
                   | null -> None
                   | _ -> Some c
        }

        /// <summary>
        /// Gets the reflog entries for the HEAD.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns HEAD reflog entries.</returns>
        member repository.getHeadReflogs(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetHeadReflogsAsync(
                repository, WeakReference(repository), unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the working directory status.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the working directory status.</returns>
        member repository.getWorkingDirectoryStatus(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
                repository, Glob.nothingFilter, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the working directory status with an override path filter.
        /// </summary>
        /// <param name="overrideGlobFilter">A predicate override glob filter function.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the filtered working directory status.</returns>
        member repository.getWorkingDirectoryStatus(
            overrideGlobFilter: GlobFilter, ?ct: CancellationToken) =
            StructuredRepositoryFacade.GetWorkingDirectoryStatusAsync(
                repository, overrideGlobFilter, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all worktrees associated with the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all worktrees.</returns>
        member repository.getWorktrees(?ct: CancellationToken) =
            WorktreeAccessor.GetStructuredWorktreesAsync(
                repository, unwrapCT ct).asAsync()

    type Branch with
        /// <summary>
        /// Gets the HEAD commit of the branch.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the HEAD commit.</returns>
        member branch.getHeadCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                branch, unwrapCT ct).asAsync()

    type Commit with
        /// <summary>
        /// Gets the primary parent commit, if available.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an optional primary parent commit.</returns>
        member commit.getPrimaryParentCommit(?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetPrimaryParentAsync(
                commit, unwrapCT ct).asAsync()
            return match c with
                   | null -> None
                   | _ -> Some c
        }

        /// <summary>
        /// Gets all parent commits.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all parent commits.</returns>
        member commit.getParentCommits(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetParentsAsync(
                commit, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets the tree root of the commit.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tree root.</returns>
        member commit.getTreeRoot(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetTreeAsync(
                commit, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets the commit message.
        /// </summary>
        /// <returns>The commit message.</returns>
        member commit.getMessage() =
            commit.message
            
    type Tag with
        /// <summary>
        /// Gets the commit referenced by the tag.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the commit.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the tag type is not a commit.</exception>
        member tag.getCommit(?ct: CancellationToken) =
            match tag.Type with
            | ObjectTypes.Commit -> StructuredRepositoryFacade.GetCommitAsync(tag, unwrapCT ct).asAsync()
            | _ -> raise (InvalidOperationException $"Could not get commit: Type={tag.Type}")
            
        /// <summary>
        /// Gets the annotation of the tag.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tag annotation.</returns>
        member tag.getAnnotation(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetAnnotationAsync(tag, unwrapCT ct).asAsync()
       
    type Stash with
        /// <summary>
        /// Gets the commit associated with the stash.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the stash commit.</returns>
        member stash.getCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                stash, unwrapCT ct).asAsync()
            
    type ReflogEntry with
        /// <summary>
        /// Gets the current commit of the reflog entry.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the current commit.</returns>
        member reflog.getCurrentCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.Commit, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets the old commit of the reflog entry.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the old commit.</returns>
        member reflog.getOldCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.OldCommit, unwrapCT ct).asAsync()

    type TreeBlobEntry with
        /// <summary>
        /// Opens the blob content stream.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the blob stream.</returns>
        member entry.openBlob(?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenBlobAsync(
                entry, unwrapCT ct).asAsync()

    type TreeSubModuleEntry with
        /// <summary>
        /// Opens the submodule repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the submodule repository.</returns>
        member entry.openSubModule(?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenSubModuleAsync(
                entry, unwrapCT ct).asAsync()

    /// <summary>
    /// Active pattern for deconstructing a StructuredRepository into its component parts.
    /// </summary>
    /// <param name="repository">The repository to deconstruct.</param>
    /// <returns>A tuple containing the Git path, remote URLs, optional HEAD, all branches, and tags.</returns>
    let (|StructuredRepository|) (repository: StructuredRepository) =
        (repository.GitPath,
         repository.RemoteUrls,
         (match repository.head with
          | null -> None
          | _ -> Some repository.head),
         repository.branchesAll,
         repository.tags)

    /// <summary>
    /// Active pattern for deconstructing a Branch into its component parts.
    /// </summary>
    /// <param name="branch">The branch to deconstruct.</param>
    /// <returns>A tuple containing the name and HEAD hash.</returns>
    let (|Branch|) (branch: Branch) =
        (branch.Name, branch.Head)

    /// <summary>
    /// Active pattern for deconstructing a Commit into its component parts.
    /// </summary>
    /// <param name="commit">The commit to deconstruct.</param>
    /// <returns>A tuple containing the hash, author, committer, subject, and body.</returns>
    let (|Commit|) (commit: Commit) =
        (commit.Hash, commit.Author, commit.Committer, commit.Subject, commit.Body)

    /// <summary>
    /// Active pattern for deconstructing a Tag into its component parts.
    /// </summary>
    /// <param name="tag">The tag to deconstruct.</param>
    /// <returns>A tuple containing the optional tag hash, type, object hash, and name.</returns>
    let (|Tag|) (tag: Tag) =
        ((match tag.TagHash.HasValue with
         | false -> None
         | _ -> Some tag.TagHash.Value),
         tag.Type, tag.ObjectHash, tag.Name)

    /// <summary>
    /// Active pattern for deconstructing an Annotation into its component parts.
    /// </summary>
    /// <param name="annotation">The annotation to deconstruct.</param>
    /// <returns>A tuple containing the optional tagger and optional message.</returns>
    let (|Annotation|) (annotation: Annotation) =
        ((match annotation.Tagger.HasValue with
          | false -> None
          | _ -> Some annotation.Tagger.Value),
         (match annotation.Message with
          | null -> None
          | _ -> Some annotation.Message))

    /// <summary>
    /// Active pattern for deconstructing a WorkingDirectoryFile into its component parts.
    /// </summary>
    /// <param name="file">The working directory file to deconstruct.</param>
    /// <returns>A tuple containing the path, status, optional index hash, and optional working tree hash.</returns>
    let (|WorkingDirectoryFile|) (file: WorkingDirectoryFile) =
        (file.Path, file.Status, file.IndexHash |> wrapOptionV, file.WorkingTreeHash |> wrapOptionV)

    /// <summary>
    /// Active pattern for deconstructing a WorkingDirectoryStatus into its component parts.
    /// </summary>
    /// <param name="status">The working directory status to deconstruct.</param>
    /// <returns>A tuple containing staged files, unstaged files, and untracked files.</returns>
    let (|WorkingDirectoryStatus|) (status: WorkingDirectoryStatus) =
        (status.StagedFiles, status.UnstagedFiles, status.UntrackedFiles)

    /// <summary>
    /// Active pattern for deconstructing a Worktree into its component parts.
    /// </summary>
    /// <param name="worktree">The worktree to deconstruct.</param>
    /// <returns>A tuple containing the name, path, optional HEAD, optional branch, and status.</returns>
    let (|Worktree|) (worktree: Worktree) =
        (worktree.Name, worktree.Path, worktree.Head |> wrapOptionV, 
         (match worktree.Branch with
          | null -> None
          | _ -> Some worktree.Branch), worktree.Status)
