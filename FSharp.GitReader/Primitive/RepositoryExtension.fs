////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open System.Runtime.CompilerServices
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
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getCurrentHeadReference(?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(
                repository, unwrapCT ct) |> asOptionAsync

        /// <summary>
        /// Gets the head reference for the specified branch.
        /// </summary>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the branch head reference.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getBranchHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchHeadReferenceAsync(
                repository, branchName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all head references for the specified branch.
        /// </summary>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all branch head references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getBranchAllHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchAllHeadReferenceAsync(
                repository, branchName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the reference for the specified tag.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tag reference.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getTagReference(tagName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetTagReferenceAsync(
                repository, tagName, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the commit object for the specified hash.
        /// </summary>
        /// <param name="commit">The hash of the commit.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an optional commit object.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) =
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
            RepositoryAccessor.ReadCommitAsync(repository, commit, unwrapCT ct).AsTask() |> asOptionAsync
#else
            RepositoryAccessor.ReadCommitAsync(repository, commit, unwrapCT ct) |> asOptionAsync
#endif

        /// <summary>
        /// Gets the tag object for the specified tag reference.
        /// </summary>
        /// <param name="tag">The tag reference.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tag object.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getTag(tag: PrimitiveTagReference, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetTagAsync(repository, tag, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all branch head references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all branch head references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Branches, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all remote branch head references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all remote branch head references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getRemoteBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.RemoteBranches, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all tag references.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all tag references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getTagReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadTagReferencesAsync(
                repository, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets all stashes in the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all stashes.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getStashes(?ct: CancellationToken) =
            RepositoryAccessor.ReadStashesAsync(
                repository, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the reflog entries related to the specified reference.
        /// </summary>
        /// <param name="reference">The reference to get reflog entries for.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns reflog entries.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getRelatedReflogs(reference: PrimitiveReference, ?ct: CancellationToken) =
            RepositoryAccessor.ReadReflogEntriesAsync(
                repository, reference, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all branch head references that point to the specified commit.
        /// </summary>
        /// <param name="commitHash">The commit hash to find related branches for.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns related branch head references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getRelatedBranchHeadReferences(commitHash: Hash, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetRelatedBranchHeadReferencesAsync(
                repository, commitHash, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all tag references that point to the specified commit.
        /// </summary>
        /// <param name="commitHash">The commit hash to find related tags for.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns related tag references.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getRelatedTagReferences(commitHash: Hash, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetRelatedTagReferencesAsync(
                repository, commitHash, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets all tags that point to the specified commit.
        /// </summary>
        /// <param name="commitHash">The commit hash to find related tags for.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns related tags.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getRelatedTags(commitHash: Hash, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetRelatedTagsAsync(
                repository, commitHash, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the tree object for the specified hash.
        /// </summary>
        /// <param name="tree">The hash of the tree.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the tree object.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getTree(tree: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadTreeAsync(repository, tree, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Opens a blob object for the specified hash.
        /// </summary>
        /// <param name="blob">The hash of the blob.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the blob stream.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.openBlob(blob: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.OpenBlobAsync(repository, blob, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the working directory status.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the working directory status.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getWorkingDirectoryStatus(?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetWorkingDirectoryStatusAsync(
                repository, unwrapCT ct).asAsync()

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getUntrackedFiles(
            workingDirectoryStatus: PrimitiveWorkingDirectoryStatus,
            ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
                repository, workingDirectoryStatus, Glob.nothingFilter, unwrapCT ct).asAsync()

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getUntrackedFiles(
            workingDirectoryStatus: PrimitiveWorkingDirectoryStatus,
            overrideGlobFilter: GlobFilter,
            ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetUntrackedFilesAsync(
                repository, workingDirectoryStatus, overrideGlobFilter, unwrapCT ct).asAsync()
                
        /// <summary>
        /// Gets all worktrees associated with the repository.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns all worktrees.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member repository.getWorktrees(?ct: CancellationToken) =
            WorktreeAccessor.GetPrimitiveWorktreesAsync(repository, unwrapCT ct).asAsync()

    type PrimitiveWorktree with
        /// <summary>
        /// Gets the HEAD reference of the worktree.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the HEAD reference.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member worktree.getHead(?ct: CancellationToken) =
            WorktreeAccessor.GetWorktreeHeadAsync(worktree, unwrapCT ct).asAsync()

        /// <summary>
        /// Gets the branch of the worktree.
        /// </summary>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns the branch.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member worktree.getBranch(?ct: CancellationToken) =
            WorktreeAccessor.GetWorktreeBranchAsync(worktree, unwrapCT ct).asAsync()

    type PrimitiveCommit with
        /// <summary>
        /// Cracks the message of the specified commit into its subject and body.
        /// </summary>
        /// <returns>A tuple containing the subject and body of the commit message.</returns>
        /// <remarks>
        /// The subject line is the first line of the commit message. It is nearly as git command `git log --format=%s`.
        /// The body is the rest of the commit message after the first blank line. It is nearly as git command `git log --format=%b`.
        /// </remarks>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member commit.crackMessage() =
            let mutable subject: string = Unchecked.defaultof<string>
            let mutable body: string = Unchecked.defaultof<string>
            Utilities.CrackGitMessage(commit.Message, &subject, &body)
            (subject, body)
    
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
    /// <returns>A tuple containing staged files and unstaged files</returns>
    let (|PrimitiveWorkingDirectoryStatus|) (status: PrimitiveWorkingDirectoryStatus) =
        (status.StagedFiles, status.UnstagedFiles)

    /// <summary>
    /// Active pattern for deconstructing a PrimitiveWorktree into its component parts.
    /// </summary>
    /// <param name="worktree">The worktree to deconstruct.</param>
    /// <returns>A tuple containing the name, path, and status.</returns>
    let (|PrimitiveWorktree|) (worktree: PrimitiveWorktree) =
        (worktree.Name, worktree.Path, worktree.Status)
