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
    public static Task<Reference> GetCurrentHeadReferenceAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCurrentHeadReferenceAsync(repository, ct);

    public static Task<Reference> GetBranchHeadReferenceAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default) =>
        RepositoryFacade.GetBranchHeadReferenceAsync(repository, branchName, ct);

    public static Task<Reference> GetRemoteBranchHeadReferenceAsync(
        this Repository repository,
        string branchName, CancellationToken ct = default) =>
        RepositoryFacade.GetRemoteBranchHeadReferenceAsync(repository, branchName, ct);

    public static Task<Reference> GetTagReferenceAsync(
        this Repository repository,
        string tagName, CancellationToken ct = default) =>
        RepositoryFacade.GetTagReferenceAsync(repository, tagName, ct);

    public static Task<Commit> GetCommitAsync(
        this Repository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryAccessor.ReadCommitAsync(repository, commit, ct);

    public static async Task<Tag> GetTagAsync(
        this Repository repository,
        Reference tag, CancellationToken ct = default) =>
        await RepositoryAccessor.ReadTagAsync(repository, tag, ct) is { } t ?
            t : Tag.Create(tag, ObjectTypes.Commit, tag.Name, null, null);

    public static Task<Reference[]> GetBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, "heads", ct);

    public static Task<Reference[]> GetRemoteBranchHeadReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, "remotes", ct);

    public static Task<Reference[]> GetTagReferencesAsync(
        this Repository repository,
        CancellationToken ct = default) =>
        RepositoryAccessor.ReadReferencesAsync(repository, "tags", ct);
}
