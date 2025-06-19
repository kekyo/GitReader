////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open GitReader
open GitReader.Internal
open GitReader.Primitive
open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Provides F#-specific extension methods for primitive repository operations.
/// </summary>
[<AutoOpen>]
module public RepositoryExtension =

    type PrimitiveRepository with
        /// <summary>
        /// Gets the current HEAD reference of the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an optional current HEAD reference.</returns>
        member repository.getCurrentHeadReference(?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(
                repository, unwrapCT ct) |> asOptionAsync

        /// <summary>
        /// Gets the head reference for the specified branch.
        /// </summary>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the branch head reference.</returns>
        member repository.getBranchHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchHeadReferenceAsync(
                repository, branchName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all head references for the specified branch.
        /// </summary>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all branch head references.</returns>
        member repository.getBranchAllHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchAllHeadReferenceAsync(
                repository, branchName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the reference for the specified tag.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tag reference.</returns>
        member repository.getTagReference(tagName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetTagReferenceAsync(
                repository, tagName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the commit object for the specified hash.
        /// </summary>
        /// <param name="commit">The hash of the commit.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an optional commit object.</returns>
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadCommitAsync(repository, commit, unwrapCT ct) |> asOptionAsync

        /// <summary>
        /// Gets the tag object for the specified tag reference.
        /// </summary>
        /// <param name="tag">The tag reference.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tag object.</returns>
        member repository.getTag(tag: PrimitiveTagReference, ?ct: CancellationToken) = async {
            let! t = RepositoryAccessor.ReadTagAsync(repository, tag.ObjectOrCommitHash, unwrapCT ct) |> asOptionAsync
            return match t with
                   | Some t -> t
                   | None -> PrimitiveTag(tag.ObjectOrCommitHash, ObjectTypes.Commit, tag.Name, System.Nullable(), null)
        }

        /// <summary>
        /// Gets all branch head references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all branch head references.</returns>
        member repository.getBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Branches, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all remote branch head references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all remote branch head references.</returns>
        member repository.getRemoteBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.RemoteBranches, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all tag references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all tag references.</returns>
        member repository.getTagReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadTagReferencesAsync(
                repository, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets all stashes in the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all stashes.</returns>
        member repository.getStashes(?ct: CancellationToken) =
            RepositoryAccessor.ReadStashesAsync(
                repository, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the reflog entries related to the specified reference.
        /// </summary>
        /// <param name="reference">The reference to get reflog entries for.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns reflog entries.</returns>
        member repository.getRelatedReflogs(reference: PrimitiveReference, ?ct: CancellationToken) =
            RepositoryAccessor.ReadReflogEntriesAsync(
                repository, reference, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the tree object for the specified hash.
        /// </summary>
        /// <param name="tree">The hash of the tree.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tree object.</returns>
        member repository.getTree(tree: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadTreeAsync(repository, tree, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Opens a blob object for the specified hash.
        /// </summary>
        /// <param name="blob">The hash of the blob.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the blob stream.</returns>
        member repository.openBlob(blob: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.OpenBlobAsync(repository, blob, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the working directory status.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the working directory status.</returns>
        member repository.getWorkingDirectoryStatus(?ct: CancellationToken) =
            WorkingDirectoryAccessor.GetPrimitiveWorkingDirectoryStatusAsync(
                repository, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets the working directory status with a custom path filter.
        /// </summary>
        /// <param name="overridePathFilter">The custom path filter to apply.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the filtered working directory status.</returns>
        member repository.getWorkingDirectoryStatusWithFilter(overridePathFilter: GlobFilter, ?ct: CancellationToken) =
            WorkingDirectoryAccessor.GetPrimitiveWorkingDirectoryStatusWithFilterAsync(
                repository, overridePathFilter, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets all worktrees associated with the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all worktrees.</returns>
        member repository.getWorktrees(?ct: CancellationToken) =
            WorktreeAccessor.GetPrimitiveWorktreesAsync(repository, unwrapCT ct).asAsync()

    type PrimitiveWorktree with
        /// <summary>
        /// Gets the HEAD reference of the worktree.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the HEAD reference.</returns>
        member worktree.getHead(?ct: CancellationToken) =
            WorktreeAccessor.GetWorktreeHeadAsync(worktree, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the branch of the worktree.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the branch.</returns>
        member worktree.getBranch(?ct: CancellationToken) =
            WorktreeAccessor.GetWorktreeBranchAsync(worktree, unwrapCT ct).asAsync()

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveRepository into its component parts.
    /// </summary>
    /// <param name="repository">The repository to deconstruct.</param>
    /// <returns>A tuple containing the Git path and remote URLs.</returns>
    let (|PrimitiveRepository|) (repository: PrimitiveRepository) =
        (repository.GitPath, repository.RemoteUrls)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveReference into its component parts.
    /// </summary>
    /// <param name="reference">The reference to deconstruct.</param>
    /// <returns>A tuple containing the name, relative path, and target.</returns>
    let (|PrimitiveReference|) (reference: PrimitiveReference) =
        (reference.Name, reference.RelativePath, reference.Target)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveTagReference into its component parts.
    /// </summary>
    /// <param name="tagReference">The tag reference to deconstruct.</param>
    /// <returns>A tuple containing the name, relative path, object/commit hash, and commit hash.</returns>
    let (|PrimitiveTagReference|) (tagReference: PrimitiveTagReference) =
        (tagReference.Name, tagReference.RelativePath, tagReference.ObjectOrCommitHash, tagReference.CommitHash)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveCommit into its component parts.
    /// </summary>
    /// <param name="commit">The commit to deconstruct.</param>
    /// <returns>A tuple containing the hash, tree root, author, committer, parents, and message.</returns>
    let (|PrimitiveCommit|) (commit: PrimitiveCommit) =
        (commit.Hash, commit.TreeRoot, commit.Author, commit.Committer, commit.Parents, commit.Message)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveTag into its component parts.
    /// </summary>
    /// <param name="tag">The tag to deconstruct.</param>
    /// <returns>A tuple containing the hash, type, name, optional tagger, and optional message.</returns>
    let (|PrimitiveTag|) (tag: PrimitiveTag) =
        (tag.Hash, tag.Type, tag.Name, tag.Tagger |> wrapOptionV, tag.Message |> wrapOption)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveReflogEntry into its component parts.
    /// </summary>
    /// <param name="reflog">The reflog entry to deconstruct.</param>
    /// <returns>A tuple containing the old hash, current hash, committer, and message.</returns>
    let (|PrimitiveReflogEntry|) (reflog: PrimitiveReflogEntry) =
        (reflog.Old, reflog.Current, reflog.Committer, reflog.Message)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveTree into its component parts.
    /// </summary>
    /// <param name="tree">The tree to deconstruct.</param>
    /// <returns>A tuple containing the hash and children.</returns>
    let (|PrimitiveTree|) (tree: PrimitiveTree) =
        (tree.Hash, tree.Children)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveTreeEntry into its component parts.
    /// </summary>
    /// <param name="entry">The tree entry to deconstruct.</param>
    /// <returns>A tuple containing the hash, name, and modes.</returns>
    let (|PrimitiveTreeEntry|) (entry: PrimitiveTreeEntry) =
        (entry.Hash, entry.Name, entry.Modes)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveWorkingDirectoryFile into its component parts.
    /// </summary>
    /// <param name="file">The working directory file to deconstruct.</param>
    /// <returns>A tuple containing the path, status, optional index hash, and optional working tree hash.</returns>
    let (|PrimitiveWorkingDirectoryFile|) (file: PrimitiveWorkingDirectoryFile) =
        (file.Path, file.Status, file.IndexHash |> wrapOptionV, file.WorkingTreeHash |> wrapOptionV)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveWorkingDirectoryStatus into its component parts.
    /// </summary>
    /// <param name="status">The working directory status to deconstruct.</param>
    /// <returns>A tuple containing staged files, unstaged files, and untracked files.</returns>
    let (|PrimitiveWorkingDirectoryStatus|) (status: PrimitiveWorkingDirectoryStatus) =
        (status.StagedFiles, status.UnstagedFiles, status.UntrackedFiles)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveWorktree into its component parts.
    /// </summary>
    /// <param name="worktree">The worktree to deconstruct.</param>
    /// <returns>A tuple containing the name, path, and status.</returns>
    let (|PrimitiveWorktree|) (worktree: PrimitiveWorktree) =
        (worktree.Name, worktree.Path, worktree.Status)
