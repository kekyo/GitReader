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
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = repository.Head;

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetCommitDirectly()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetBranch()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = repository.Branches["master"];

        await Verifier.Verify(branch);
    }

    [Test]
    public async Task GetRemoteBranch()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = repository.RemoteBranches["origin/devel"];

        await Verifier.Verify(branch);
    }

    [Test]
    public async Task GetTag()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var tag = repository.Tags["2.0.0"];

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetTag2()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var tag = repository.Tags["0.9.6"];

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetBranchesFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "9bb78d13405cab568d3e213130f31beda1ce21d1");
        var branches = commit.Branches;

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task GetTagsFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "f64de5e3ad34528757207109e68f626bf8cc1a31");
        var tags = commit.Tags;

        await Verifier.Verify(tags);
    }

    [Test]
    public async Task GetRemoteBranchesFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "f690f0e7bf703582a1fad7e6f1c2d1586390f43d");
        var branches = commit.RemoteBranches;

        await Verifier.Verify(branches);
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = repository.Branches["master"];

        Console.WriteLine($"Name: {branch.Name}");

        var current = branch.Head;
        var commits = new List<Commit>();
        while (true)
        {
            commits.Add(current);

            // Get parent commits.
            var parents = await current.GetParentsAsync();

            // Bottom of branch.
            if (parents.Length == 0)
            {
                break;
            }

            // Get primary parent.
            current = parents[0];
        }

        await Verifier.Verify(commits.ToArray());
    }
}
