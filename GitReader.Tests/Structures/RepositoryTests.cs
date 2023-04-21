////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
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

namespace GitReader.Structures;

public sealed class RepositoryTests
{
    [Test]
    public async Task GetCurrentHead()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCurrentHeadAsync();

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetBranch()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = await repository.GetBranchAsync("master");

        await Verifier.Verify(branch);
    }

    [Test]
    public async Task GetRemoteBranch()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = await repository.GetRemoteBranchAsync("origin/devel");

        await Verifier.Verify(branch);
    }

    [Test]
    public async Task GetTag()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var tag = await repository.GetTagAsync("2.0.0");

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetTag2()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var tag = await repository.GetTagAsync("0.9.6");

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetBranches()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branches = await repository.GetBranchesAsync();

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task GetRemoteBranches()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branches = await repository.GetRemoteBranchesAsync();

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task GetTags()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var tags = await repository.GetTagsAsync();

        await Verifier.Verify(tags);
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = await repository.GetBranchAsync("master");

        Console.WriteLine($"Name: {branch.Name}");

        var current = branch.Head;
        var commits = new List<Commit>();
        while (true)
        {
            commits.Add(current);

            // Bottom of branch.
            if (current.GetParentCount() == 0)
            {
                break;
            }

            // Get primary parent.
            current = await current.GetParentAsync(0);
        }

        await Verifier.Verify(commits.ToArray());
    }
}
