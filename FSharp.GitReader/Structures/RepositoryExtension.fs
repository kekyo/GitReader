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

[<AutoOpen>]
module public RepositoryExtension =

    type StructuredRepository with
        member repository.getCurrentHead() =
            match repository.Head with
            | null -> None
            | _ -> Some repository.Head
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetCommitDirectlyAsync(
                repository, commit, unwrapCT ct).asAsync()
            return match c with
                   | null -> None
                   | _ -> Some c
        }
        member repository.getHeadReflogs(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetHeadReflogsAsync(
                repository, WeakReference(repository), unwrapCT ct).asAsync()
        member repository.getWorkingDirectoryStatus(?ct: CancellationToken) =
            WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusAsync(
                repository, WeakReference(repository), unwrapCT ct).asAsync()
        member repository.getWorkingDirectoryStatusWithFilter(overridePathFilter: FilterDecisionDelegate, ?ct: CancellationToken) =
            WorkingDirectoryAccessor.GetStructuredWorkingDirectoryStatusWithFilterAsync(
                repository, WeakReference(repository), overridePathFilter, unwrapCT ct).asAsync()
        member repository.getWorktrees(?ct: CancellationToken) =
            WorktreeAccessor.GetStructuredWorktreesAsync(
                repository, WeakReference(repository), unwrapCT ct).asAsync()

    type Branch with
        member branch.getHeadCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                branch, unwrapCT ct).asAsync()

    type Commit with
        member commit.getPrimaryParentCommit(?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetPrimaryParentAsync(
                commit, unwrapCT ct).asAsync()
            return match c with
                   | null -> None
                   | _ -> Some c
        }
        member commit.getParentCommits(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetParentsAsync(
                commit, unwrapCT ct).asAsync()
        member commit.getTreeRoot(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetTreeAsync(
                commit, unwrapCT ct).asAsync()
        member commit.getMessage() =
            commit.message
            
    type Tag with
        member tag.getCommit(?ct: CancellationToken) =
            match tag.Type with
            | ObjectTypes.Commit -> StructuredRepositoryFacade.GetCommitAsync(tag, unwrapCT ct).asAsync()
            | _ -> raise (InvalidOperationException $"Could not get commit: Type={tag.Type}")
        member tag.getAnnotation(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetAnnotationAsync(tag, unwrapCT ct).asAsync()
       
    type Stash with
        member stash.getCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                stash, unwrapCT ct).asAsync()
            
    type ReflogEntry with
        member reflog.getCurrentCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.Commit, unwrapCT ct).asAsync()
        member reflog.getOldCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.OldCommit, unwrapCT ct).asAsync()

    type TreeBlobEntry with
        member entry.openBlob(?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenBlobAsync(
                entry, unwrapCT ct).asAsync()

    type TreeSubModuleEntry with
        member entry.openSubModule(?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenSubModuleAsync(
                entry, unwrapCT ct).asAsync()

    let (|StructuredRepository|) (repository: StructuredRepository) =
        (repository.GitPath,
         repository.RemoteUrls,
         (match repository.head with
          | null -> None
          | _ -> Some repository.head),
         repository.branchesAll,
         repository.tags)

    let (|Branch|) (branch: Branch) =
        (branch.Name, branch.Head)

    let (|Commit|) (commit: Commit) =
        (commit.Hash, commit.Author, commit.Committer, commit.Subject, commit.Body)

    let (|Tag|) (tag: Tag) =
        ((match tag.TagHash.HasValue with
         | false -> None
         | _ -> Some tag.TagHash.Value),
         tag.Type, tag.ObjectHash, tag.Name)

    let (|Annotation|) (annotation: Annotation) =
        ((match annotation.Tagger.HasValue with
          | false -> None
          | _ -> Some annotation.Tagger.Value),
         (match annotation.Message with
          | null -> None
          | _ -> Some annotation.Message))

    let (|WorkingDirectoryFile|) (file: WorkingDirectoryFile) =
        (file.Path, file.Status, file.IndexHash |> wrapOptionV, file.WorkingTreeHash |> wrapOptionV)

    let (|WorkingDirectoryStatus|) (status: WorkingDirectoryStatus) =
        (status.StagedFiles, status.UnstagedFiles, status.UntrackedFiles)

    let (|Worktree|) (worktree: Worktree) =
        (worktree.Name, worktree.Path, worktree.Head |> wrapOptionV, 
         (match worktree.Branch with
          | null -> None
          | _ -> Some worktree.Branch), worktree.Status)
