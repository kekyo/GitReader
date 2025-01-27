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
open System
open System.Threading

[<AutoOpen>]
module public RepositoryExtension =

    type PrimitiveRepository with
        member repository.getCurrentHeadReference(?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(
                repository, unwrapCT ct) |> asAsync

        member repository.getBranchHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchHeadReferenceAsync(
                repository, branchName, unwrapCT ct) |> Async.AwaitTask

        member repository.getBranchAllHeadReference(branchName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetBranchAllHeadReferenceAsync(
                repository, branchName, unwrapCT ct) |> Async.AwaitTask

        member repository.getTagReference(tagName: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.GetTagReferenceAsync(
                repository, tagName, unwrapCT ct) |> Async.AwaitTask

        member repository.getCommit(commit: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadCommitAsync(repository, commit, unwrapCT ct) |> asAsync

        member repository.getTag(tag: PrimitiveTagReference, ?ct: CancellationToken) = async {
            let! t = RepositoryAccessor.ReadTagAsync(repository, tag.ObjectOrCommitHash, unwrapCT ct) |> asAsync
            return match t with
                   | Some t -> t
                   | None -> PrimitiveTag(tag.ObjectOrCommitHash, ObjectTypes.Commit, tag.Name, Nullable(), null)
        }

        member repository.getBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.Branches, unwrapCT ct) |> Async.AwaitTask

        member repository.getRemoteBranchHeadReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadReferencesAsync(
                repository, ReferenceTypes.RemoteBranches, unwrapCT ct) |> Async.AwaitTask

        member repository.getTagReferences(?ct: CancellationToken) =
            RepositoryAccessor.ReadTagReferencesAsync(
                repository, unwrapCT ct) |> Async.AwaitTask
                
        member repository.getStashes(?ct: CancellationToken) =
            RepositoryAccessor.ReadStashesAsync(
                repository, unwrapCT ct) |> Async.AwaitTask

        member repository.getRelatedReflogs(reference: PrimitiveReference, ?ct: CancellationToken) =
            RepositoryAccessor.ReadReflogEntriesAsync(
                repository, reference, unwrapCT ct) |> Async.AwaitTask

        member repository.getTree(tree: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.ReadTreeAsync(repository, tree, unwrapCT ct) |> Async.AwaitTask
                
        member repository.openBlob(blob: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.OpenBlobAsync(repository, blob, unwrapCT ct) |> Async.AwaitTask

    let (|PrimitiveRepository|) (repository: PrimitiveRepository) =
        (repository.GitPath, repository.RemoteUrls)

    let (|PrimitiveReference|) (reference: PrimitiveReference) =
        (reference.Name, reference.RelativePath, reference.Target)

    let (|PrimitiveTagReference|) (tagReference: PrimitiveTagReference) =
        (tagReference.Name, tagReference.RelativePath, tagReference.ObjectOrCommitHash, tagReference.CommitHash)

    let (|PrimitiveCommit|) (commit: PrimitiveCommit) =
        (commit.Hash, commit.TreeRoot, commit.Author, commit.Committer, commit.Parents, commit.Message)

    let (|PrimitiveTag|) (tag: PrimitiveTag) =
        (tag.Hash, tag.Type, tag.Name, tag.Tagger |> wrapOptionV, tag.Message |> wrapOption)

    let (|PrimitiveReflogEntry|) (reflog: PrimitiveReflogEntry) =
        (reflog.Old, reflog.Current, reflog.Committer, reflog.Message)

    let (|PrimitiveTree|) (tree: PrimitiveTree) =
        (tree.Hash, tree.Children)

    let (|PrimitiveTreeEntry|) (entry: PrimitiveTreeEntry) =
        (entry.Hash, entry.Name, entry.Modes)
