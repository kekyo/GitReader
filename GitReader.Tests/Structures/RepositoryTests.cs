////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        var head = repository.GetCurrentHead();

        await Verifier.Verify(head);
    }

    [Test]
    public async Task GetCommitDirectly()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task CommitNotFound()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "0000000000000000000000000000000000000000");

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
        var branches = commit!.Branches;

        await Verifier.Verify(branches.OrderBy(b => b.Name).ToArray());
    }

    [Test]
    public async Task GetTagsFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "f64de5e3ad34528757207109e68f626bf8cc1a31");
        var tags = commit!.Tags;

        await Verifier.Verify(tags.OrderBy(t => t.Name).ToArray());
    }

    [Test]
    public async Task GetRemoteBranchesFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "f690f0e7bf703582a1fad7e6f1c2d1586390f43d");
        var branches = commit!.RemoteBranches;

        await Verifier.Verify(branches.OrderBy(br => br.Name).ToArray());
    }

    [Test]
    public async Task GetParentCommits()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "dc45301eeb49ec94ee043755124802497d0079ec");
        var parents = await commit!.GetParentCommitsAsync();

        await Verifier.Verify(parents);
    }

    [Test]
    public async Task GetTreeRoot()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var treeRoot = await commit!.GetTreeRootAsync();

        await Verifier.Verify(treeRoot);
    }

    [Test]
    public async Task OpenBlob()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var tree = await commit!.GetTreeRootAsync();

        var blob = tree.Children.OfType<TreeBlobEntry>().
            First(child => child.Name == "build-nupkg.bat");

        using var blobStream = await blob.OpenBlobAsync();
        var blobText = new StreamReader(blobStream).ReadToEnd();

        await Verifier.Verify(blobText);
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.BasePath);

        var branch = repository.Branches["master"];

        var commits = new List<Commit>();
        var current = branch.Head;

        while (current != null)
        {
            commits.Add(current);

            // Get primary parent commit.
            current = await current.GetPrimaryParentCommitAsync();
        }

        await Verifier.Verify(commits.ToArray());
    }
}
