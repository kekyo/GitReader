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
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;
using VerifyTests;

namespace GitReader.Primitive;

public sealed class RepositoryTests
{
    [Test]
    public async Task GetCommitDirectly()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task CommitNotFound()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var commit = await repository.GetCommitAsync(
            "0000000000000000000000000000000000000000");

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetCurrentHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var headref = await repository.GetCurrentHeadReferenceAsync();
        var commit = await repository.GetCommitAsync(headref.Value);

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetBranchHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var headref = await repository.GetBranchHeadReferenceAsync("master");
        var commit = await repository.GetCommitAsync(headref);

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetRemoteBranchHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var headref = await repository.GetRemoteBranchHeadReferenceAsync("origin/devel");
        var commit = await repository.GetCommitAsync(headref);

        await Verifier.Verify(commit);
    }

    [Test]
    public async Task GetTag()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var tagref = await repository.GetTagReferenceAsync("2.0.0");
        var tag = await repository.GetTagAsync(tagref);

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetTag2()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var tagref = await repository.GetTagReferenceAsync("0.9.6");
        var tag = await repository.GetTagAsync(tagref);

        await Verifier.Verify(tag);
    }

    [Test]
    public async Task GetBranchHeads()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var branchrefs = await repository.GetBranchHeadReferencesAsync();

        await Verifier.Verify(branchrefs);
    }

    [Test]
    public async Task GetRemoteBranchHeads()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var branchrefs = await repository.GetRemoteBranchHeadReferencesAsync();

        await Verifier.Verify(branchrefs);
    }

    [Test]
    public async Task GetTags()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var tagrefs = await repository.GetTagReferencesAsync();
        var tags = await Task.WhenAll(
            tagrefs.Select(tagReference => repository.GetTagAsync(tagReference)));

        await Verifier.Verify(tags);
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.BasePath);

        var branchref = await repository.GetBranchHeadReferenceAsync("master");
        var commit = (await repository.GetCommitAsync(branchref))!.Value;

        var commits = new List<Commit>();
        while (true)
        {
            commits.Add(commit);

            // Bottom of branch.
            if (commit.Parents.Length == 0)
            {
                break;
            }

            // Get primary parent.
            var primary = commit.Parents[0];
            commit = (await repository.GetCommitAsync(primary))!.Value;
        }

        await Verifier.Verify(commits.ToArray());
    }
}
