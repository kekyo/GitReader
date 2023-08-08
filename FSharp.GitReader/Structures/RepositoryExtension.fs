////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open System
open System.Threading

[<AutoOpen>]
module public RepositoryExtension =

    type StructuredRepository with
        member repository.getCurrentHead() =
            match repository.Head with
            | null -> None
            | _ -> Some repository.Head
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetCommitDirectlyAsync(
                repository, commit, unwrapCT ct) |> Async.AwaitTask
            return match c with
                   | null -> None
                   | _ -> Some c
        }
        member repository.getHeadReflogs(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetHeadReflogsAsync(
                repository, new WeakReference(repository), unwrapCT ct) |> Async.AwaitTask

    type Branch with
        member branch.getHeadCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                branch, unwrapCT ct) |> Async.AwaitTask

    type Commit with
        member commit.getPrimaryParentCommit(?ct: CancellationToken) = async {
            let! c = StructuredRepositoryFacade.GetPrimaryParentAsync(
                commit, unwrapCT ct) |> Async.AwaitTask
            return match c with
                   | null -> None
                   | _ -> Some c
        }
        member commit.getParentCommits(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetParentsAsync(
                commit, unwrapCT ct) |> Async.AwaitTask
        member commit.getTreeRoot(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetTreeAsync(
                commit, unwrapCT ct) |> Async.AwaitTask
        member commit.getMessage() =
            commit.message
            
    type Tag with
        member tag.getCommit(?ct: CancellationToken) =
            match tag.Type with
            | ObjectTypes.Commit -> StructuredRepositoryFacade.GetCommitAsync(tag, unwrapCT ct) |> Async.AwaitTask
            | _ -> raise (InvalidOperationException $"Could not get commit: Type={tag.Type}")
        member tag.getAnnotation(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetAnnotationAsync(tag, unwrapCT ct) |> Async.AwaitTask
       
    type Stash with
        member stash.getCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                stash, unwrapCT ct) |> Async.AwaitTask
            
    type ReflogEntry with
        member reflog.getCurrentCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.Commit, unwrapCT ct) |> Async.AwaitTask
        member reflog.getOldCommit(?ct: CancellationToken) =
            StructuredRepositoryFacade.GetCommitAsync(
                reflog, reflog.OldCommit, unwrapCT ct) |> Async.AwaitTask

    type TreeBlobEntry with
        member entry.openBlob(?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenBlobAsync(
                entry, unwrapCT ct) |> Async.AwaitTask

    let (|StructuredRepository|) (repository: StructuredRepository) =
        (repository.GitPath,
         repository.RemoteUrls,
         (match repository.head with
          | null -> None
          | _ -> Some repository.head),
         repository.branches,
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
