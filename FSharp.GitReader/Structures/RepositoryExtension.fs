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
