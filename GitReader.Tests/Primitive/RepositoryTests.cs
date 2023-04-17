////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyNUnit;

namespace GitReader.Primitive;

public sealed class RepositoryTests
{
    [Test]
    public async Task GetCurrentHead()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var head = await repository.GetCurrentHeadAsync();
        var commit = await repository.GetCommitAsync(head);

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetBranchHead()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var head = await repository.GetBranchHeadAsync("master");
        var commit = await repository.GetCommitAsync(head);

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetBranchHeads()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branches = await repository.GetBranchHeadsAsync();

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task GetRemoteBranchHeads()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branches = await repository.GetRemoteBranchHeadsAsync();

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task GetTags()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var tags = await repository.GetTagsAsync();

        await Verifier.Verify(tags);
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = await repository.GetBranchHeadAsync("master");
        var commit = await repository.GetCommitAsync(branch);

        var commits = new List<Commit>();
        while (true)
        {
            commits.Add(commit);

            // Primary parent.
            if (commit.Parents.Length == 0)
            {
                // Bottom of branch.
                break;
            }

            var primary = commit.Parents[0];
            commit = await repository.GetCommitAsync(primary);
        }

        await Verifier.Verify(commits.ToArray());
    }
}
