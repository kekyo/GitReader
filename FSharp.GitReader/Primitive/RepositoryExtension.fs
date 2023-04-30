////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open GitReader
open GitReader.Internal
open System
open System.Threading

[<AutoOpen>]
module public RepositoryExtension =

    type Repository with
        member repository.getCurrentHeadReference(?ct: CancellationToken) =
            RepositoryFacade.GetCurrentHeadReferenceAsync(
                repository, unwrapCT ct) |> asAsync

        member repository.getBranchHeadReference(branchName: string, ?ct: CancellationToken) =
            RepositoryFacade.GetBranchHeadReferenceAsync(
                repository, branchName, unwrapCT ct) |> Async.AwaitTask

        member repository.getRemoteBranchHeadReference(branchName: string, ?ct: CancellationToken) =
            RepositoryFacade.GetRemoteBranchHeadReferenceAsync(
                repository, branchName, unwrapCT ct) |> Async.AwaitTask

        member repository.getTagReference(tagName: string, ?ct: CancellationToken) =
            RepositoryFacade.GetTagReferenceAsync(
                repository, tagName, unwrapCT ct) |> Async.AwaitTask

        member repository.getCommit(commit: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadCommitAsync(repository, commit, unwrapCT ct) |> asAsync

        member repository.getTag(tag: Reference, ?ct: CancellationToken) = async {
            let! t = RepositoryAccessor.ReadTagAsync(repository, tag, unwrapCT ct) |> asAsync
            return match t with
                   | Some t -> t
                   | None -> Tag.Create(tag, ObjectTypes.Commit, tag.Name, Nullable(), null)
        }

        member repository.getBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Branches, unwrapCT ct) |> Async.AwaitTask

        member repository.getRemoteBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.RemoteBranches, unwrapCT ct) |> Async.AwaitTask

        member repository.getTagReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Tags, unwrapCT ct) |> Async.AwaitTask

    let (|Repository|) (repository: Repository) =
        (repository.Path)

    let (|Reference|) (reference: Reference) =
        (reference.Name, reference.Target)

    let (|Commit|) (commit: Commit) =
        (commit.Hash, commit.TreeRoot, commit.Author, commit.Committer, commit.Parents, commit.Message)

    let (|Tag|) (tag: Tag) =
        (tag.Hash, tag.Type, tag.Name, tag.Tagger, tag.Message)
