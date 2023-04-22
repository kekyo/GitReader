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

namespace GitReader.Structures;

public static class RepositoryExtension
{
    public static Task<Commit> GetCommitAsync(
        this StructuredRepository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryFacade.GetCommitDirectlyAsync(repository, commit, ct);

    public static Task<Commit?> GetPrimaryParentCommitAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        RepositoryFacade.GetPrimaryParentAsync(commit, ct);

    public static Task<Commit[]> GetParentCommitsAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        RepositoryFacade.GetParentsAsync(commit, ct);
}
