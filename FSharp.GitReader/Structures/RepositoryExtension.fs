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
open System.Threading

[<AutoOpen>]
module public RepositoryExtension =

    type StructuredRepository with
        member repository.getHead() =
            match repository.head with
            | null -> None
            | _ -> Some repository.head
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) = async {
            let! c = RepositoryFacade.GetCommitDirectlyAsync(
                repository, commit, unwrapCT ct) |> Async.AwaitTask
            return match c with
                   | null -> None
                   | _ -> Some c
        }

    type Commit with
        member commit.getPrimaryParentCommit(?ct: CancellationToken) = async {
            let! c = RepositoryFacade.GetPrimaryParentAsync(
                commit, unwrapCT ct) |> Async.AwaitTask
            return match c with
                   | null -> None
                   | _ -> Some c
        }
        member commit.getParentCommits(?ct: CancellationToken) =
            RepositoryFacade.GetParentsAsync(commit, unwrapCT ct) |> Async.AwaitTask

    let (|Repository|) (repository: StructuredRepository) =
        (repository.Path,
         (match repository.head with
          | null -> None
          | _ -> Some repository.head),
         repository.branches,
         repository.remoteBranches,
         repository.tags)

    let (|Branch|) (branch: Branch) =
        (branch.Name, branch.Head)

    let (|Commit|) (commit: Commit) =
        (commit.Hash, commit.Author, commit.Committer, commit.Message)

    let (|Tag|) (tag: Tag) =
        (tag.Hash, tag.Name, tag.Type,
         (match tag.Tagger.HasValue with
          | false -> None
          | _ -> Some tag.Tagger.Value),
         (match tag.Message with
          | null -> None
          | _ -> Some tag.Message))

    let (|CommitTag|) (tag: CommitTag) =
        (tag.Hash, tag.Name,
         (match tag.Tagger.HasValue with
          | false -> None
          | _ -> Some tag.Tagger.Value),
         (match tag.Message with
          | null -> None
          | _ -> Some tag.Message),
         tag.Commit)
