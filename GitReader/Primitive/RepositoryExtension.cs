////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

public static class RepositoryExtension
{
    public static Task<PrimitiveReference?> GetCurrentHeadReferenceAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCurrentHeadReferenceAsync(repository, ct);

    public static Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default) =>
        RepositoryFacade.GetBranchHeadReferenceAsync(repository, branchName, ct);

    public static Task<PrimitiveReference> GetRemoteBranchHeadReferenceAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default) =>
        RepositoryFacade.GetRemoteBranchHeadReferenceAsync(repository, branchName, ct);

    public static Task<PrimitiveReference> GetTagReferenceAsync(
        this Repository repository,
        string tagName, CancellationToken ct = default) =>
        RepositoryFacade.GetTagReferenceAsync(repository, tagName, ct);

    public static Task<PrimitiveCommit?> GetCommitAsync(
        this Repository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryAccessor.ReadCommitAsync(repository, commit, ct);

    public static async Task<PrimitiveTag> GetTagAsync(
        this Repository repository,
        PrimitiveReference tag, CancellationToken ct = default) =>
        await RepositoryAccessor.ReadTagAsync(repository, tag, ct) is { } t ?
            t : new(tag, ObjectTypes.Commit, tag.Name, null, null);

    public static Task<PrimitiveReference[]> GetBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.Branches, ct);

    public static Task<PrimitiveReference[]> GetRemoteBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.RemoteBranches, ct);

    public static Task<PrimitiveReference[]> GetTagReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.Tags, ct);

    public static void Deconstruct(
        this Repository repository,
        out string path)
    {
        path = repository.Path;
    }

    public static void Deconstruct(
        this PrimitiveReference reference,
        out string name,
        out Hash target)
    {
        name = reference.Name;
        target = reference.Target;
    }

    public static void Deconstruct(
        this PrimitiveCommit commit,
        out Hash hash,
        out Hash treeRoot,
        out Signature author,
        out Signature committer,
        out Hash[] parents,
        out string message)
    {
        hash = commit.Hash;
        treeRoot = commit.TreeRoot;
        author = commit.Author;
        committer = commit.Committer;
        parents = commit.Parents;
        message = commit.Message;
    }

    public static void Deconstruct(
        this PrimitiveTag tag,
        out Hash hash,
        out ObjectTypes type,
        out string name,
        out Signature? tagger,
        out string? message)
    {
        hash = tag.Hash;
        type = tag.Type;
        name = tag.Name;
        tagger = tag.Tagger;
        message = tag.Message;
    }
}
