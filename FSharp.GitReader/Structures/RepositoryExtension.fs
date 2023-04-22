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
        member repository.getCommit(commit: Hash, ?ct: CancellationToken) =
            RepositoryFacade.GetCommitDirectlyAsync(
                repository, commit, unwrap ct) |> Async.AwaitTask

    type Commit with
        member commit.getPrimaryParentCommit(?ct: CancellationToken) =
            RepositoryFacade.GetPrimaryParentAsync(commit, unwrap ct) |> Async.AwaitTask
        member commit.getParentCommits(?ct: CancellationToken) =
            RepositoryFacade.GetParentsAsync(commit, unwrap ct) |> Async.AwaitTask
