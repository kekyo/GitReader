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
    public static int GetParentCount(
        this Commit commit) =>
        commit.parents.Length;

    public static async Task<Commit> GetParentAsync(
        this Commit commit,
        int index, CancellationToken ct = default)
    {
        var parent = await RepositoryAccessor.ReadCommitAsync(
            commit.repository, commit.parents[index], ct);
        return new(commit.repository, parent);
    }
}
