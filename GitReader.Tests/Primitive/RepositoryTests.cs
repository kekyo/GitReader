////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.Primitive;

public sealed class RepositoryTests
{
    [Test]
    public async Task GetCommitDirectly()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        Assert.That(commit, Is.Not.Null);
        Assert.That(commit!.Value.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"));
        Assert.That(commit.Value.TreeRoot.ToString(), Is.EqualTo("5462bf28fdc4681762057cac7704730b1c590b38"));
        Assert.That(commit.Value.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Author.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Value.Committer.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Committer.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Value.Parents.Count, Is.EqualTo(2));
        Assert.That(commit.Value.Message, Does.StartWith("Merge branch 'devel'"));
    }

    [Test]
    public async Task CommitNotFound()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "0000000000000000000000000000000000000000");

        Assert.That(commit, Is.Null);
    }

    [Test]
    public async Task GetCurrentHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var headref = await repository.GetCurrentHeadReferenceAsync();
        var commit = await repository.GetCommitAsync(headref!.Value);

        Assert.That(commit, Is.Not.Null);
        Assert.That(commit!.Value.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"));
        Assert.That(commit.Value.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Author.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Value.Committer.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Message, Does.StartWith("Added installation .NET 6 SDK on GitHub Actions."));
    }

    [Test]
    public async Task GetBranchHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var headref = await repository.GetBranchHeadReferenceAsync("master");
        var commit = await repository.GetCommitAsync(headref);

        Assert.That(commit, Is.Not.Null);
        Assert.That(commit!.Value.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"));
        Assert.That(commit.Value.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Author.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Value.Message, Does.StartWith("Added installation .NET 6 SDK on GitHub Actions."));
    }

    [Test]
    public async Task GetRemoteBranchHead()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var headref = await repository.GetBranchHeadReferenceAsync("origin/devel");
        var commit = await repository.GetCommitAsync(headref);

        Assert.That(commit, Is.Not.Null);
        Assert.That(commit!.Value.Hash.ToString(), Is.EqualTo("f2f51b6fe6076ca630ca66c5c9f451217762652a"));
        Assert.That(commit.Value.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Value.Author.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Value.Message, Does.StartWith("Updates test nuget refs."));
    }

    [Test]
    public async Task GetTag()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tagref = await repository.GetTagReferenceAsync("2.0.0");
        var tag = await repository.GetTagAsync(tagref);

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Name, Is.EqualTo("2.0.0"));
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit));
        Assert.That(tag.Hash.ToString(), Is.EqualTo("f64de5e3ad34528757207109e68f626bf8cc1a31"));
    }

    [Test]
    public async Task GetTag2()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tagref = await repository.GetTagReferenceAsync("0.9.6");
        var tag = await repository.GetTagAsync(tagref);

        Assert.That(tag.Name, Is.EqualTo("0.9.6"));
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit));
        Assert.That(tag.Hash.ToString(), Is.EqualTo("a7187601f4b4b9dacc3c78895397bb2911d190d6"));
        Assert.That(tag.Tagger, Is.Not.Null);
        Assert.That(tag.Tagger.HasValue, Is.True);
        Assert.That(tag.Tagger!.Value.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(tag.Message, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetBranchHeads()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branchrefs = await repository.GetBranchHeadReferencesAsync();

        Assert.That(branchrefs, Is.Not.Null);
        Assert.That(branchrefs.Count, Is.EqualTo(1));
        Assert.That(branchrefs[0].Name, Is.EqualTo("master"));
        Assert.That(branchrefs[0].RelativePath, Is.EqualTo("refs/heads/master"));
        Assert.That(branchrefs[0].Target.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"));
    }

    [Test]
    public async Task GetRemoteBranchHeads()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branchrefs = await repository.GetRemoteBranchHeadReferencesAsync();

        Assert.That(branchrefs, Is.Not.Null);
        Assert.That(branchrefs.Count, Is.EqualTo(7));
        
        var orderedRefs = branchrefs.OrderBy(br => br.Name).ToArray();
        Assert.That(orderedRefs[0].Name, Is.EqualTo("origin/appveyor"));
        Assert.That(orderedRefs[1].Name, Is.EqualTo("origin/devel"));
        Assert.That(orderedRefs[1].Target.ToString(), Is.EqualTo("f2f51b6fe6076ca630ca66c5c9f451217762652a"));
        Assert.That(orderedRefs[6].Name, Is.EqualTo("origin/netcore"));
    }

    [Test]
    public async Task GetTags()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tagrefs = await repository.GetTagReferencesAsync();
        var tags = await Task.WhenAll(
            tagrefs.Select(tagReference => repository.GetTagAsync(tagReference)));

        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(15));
        
        var orderedTags = tags.OrderBy(tag => tag.Name).ToArray();
        Assert.That(orderedTags[0].Name, Is.EqualTo("0.9.5"));
        Assert.That(orderedTags[1].Name, Is.EqualTo("0.9.6"));
        Assert.That(orderedTags[6].Name, Is.EqualTo("2.0.0"));
        Assert.That(orderedTags[6].Hash.ToString(), Is.EqualTo("f64de5e3ad34528757207109e68f626bf8cc1a31"));
        Assert.That(orderedTags[14].Name, Is.EqualTo("2.2.0"));
    }

    [Test]
    public async Task GetTree()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var tree = await repository.GetTreeAsync(commit!.Value.TreeRoot);

        Assert.That(tree, Is.Not.Null);
        Assert.That(tree.Hash.ToString(), Is.EqualTo("5462bf28fdc4681762057cac7704730b1c590b38"));
        Assert.That(tree.Children, Is.Not.Null);
        Assert.That(tree.Children.Count, Is.EqualTo(8));
        Assert.That(tree.Children.Any(c => c.Name == ".github"), Is.True);
        Assert.That(tree.Children.Any(c => c.Name == "build-nupkg.bat"), Is.True);
        Assert.That(tree.Children.Any(c => c.Name == "README.md"), Is.True);
    }

    [Test]
    public async Task OpenBlob()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var tree = await repository.GetTreeAsync(commit!.Value.TreeRoot);

        var blobHash = tree.Children.First(child => child.Name == "build-nupkg.bat").Hash;

        using var blobStream = await repository.OpenBlobAsync(blobHash);
        var blobText = new StreamReader(blobStream).ReadToEnd();

        Assert.That(blobText, Is.Not.Null);
        Assert.That(blobText, Does.StartWith("@echo off"));
        Assert.That(blobText, Does.Contain("rem CenterCLR.NamingFormatter"));
        Assert.That(blobText, Does.Contain("msbuild -t:pack"));
        Assert.That(blobText.Length, Is.GreaterThan(1000));
    }

    [Test]
    public async Task OpenSubModule()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test4"));

        var commit = await repository.GetCommitAsync(
            "37021d38937107d5782f063f78f502f2da14c751");

        var tree = await repository.GetTreeAsync(commit!.Value.TreeRoot);

        var subModule = tree.Children.First(child => child.Name == "GitReader");

        using var subModuleRepository = await repository.OpenSubModuleAsync(new[] { subModule });

        var subModuleCommit = await subModuleRepository.GetCommitAsync(
            "ce68b633419a8b16d642e6ea1ec3492cdbdf2584");

        Assert.That(subModuleCommit, Is.Not.Null);
        Assert.That(subModuleCommit!.Value.Hash.ToString(), Is.EqualTo("ce68b633419a8b16d642e6ea1ec3492cdbdf2584"));
        Assert.That(subModuleCommit.Value.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(subModuleCommit.Value.Message, Does.StartWith("Merge"));
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branchref = await repository.GetBranchHeadReferenceAsync("master");
        var commit = (await repository.GetCommitAsync(branchref))!.Value;

        var commits = new List<PrimitiveCommit>();
        while (true)
        {
            commits.Add(commit);

            // Bottom of branch.
            if (commit.Parents.Count == 0)
            {
                break;
            }

            // Get primary parent.
            var primary = commit.Parents[0];
            commit = (await repository.GetCommitAsync(primary))!.Value;
        }

        Assert.That(commits, Is.Not.Null);
        Assert.That(commits.Count, Is.GreaterThan(5)); // 複数のコミットがある
        Assert.That(commits[0].Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1")); // ヘッドコミット
        Assert.That(commits.Last().Parents.Count, Is.EqualTo(0)); // 最後のコミットは親がない（ルートコミット）
        Assert.That(commits.All(c => c.Author.Name == "Kouji Matsui"), Is.True); // すべて同じ作者
    }

    [Test]
    public async Task GetRemoteUrls()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        Assert.That(repository.RemoteUrls, Is.Not.Null);
        Assert.That(repository.RemoteUrls.Count, Is.EqualTo(1));
        Assert.That(repository.RemoteUrls.ContainsKey("origin"), Is.True);
        Assert.That(repository.RemoteUrls["origin"], Is.EqualTo("https://github.com/kekyo/CenterCLR.NamingFormatter"));
    }

    [Test]
    public async Task GetRemoteUrls2()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test3"));

        Assert.That(repository.RemoteUrls, Is.Not.Null);
        Assert.That(repository.RemoteUrls.Count, Is.EqualTo(3));
        Assert.That(repository.RemoteUrls.ContainsKey("origin"), Is.True);
        Assert.That(repository.RemoteUrls.ContainsKey("test1"), Is.True);
        Assert.That(repository.RemoteUrls.ContainsKey("test2"), Is.True);
        Assert.That(repository.RemoteUrls["origin"], Is.EqualTo("https://github.com/kekyo/GitReader.Test3"));
    }

    [Test]
    public async Task GetStashes()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var stashes = await repository.GetStashesAsync();

        Assert.That(stashes, Is.Not.Null);
        Assert.That(stashes.Count, Is.EqualTo(2));
        
        var orderedStashes = stashes.OrderByDescending(stash => stash.Committer.Date).ToArray();
        Assert.That(orderedStashes[0].Committer.Name, Is.EqualTo("Julien Richard"));
        Assert.That(orderedStashes[0].Message, Is.EqualTo("On master: Stash with custom message"));
        Assert.That(orderedStashes[1].Message, Does.StartWith("WIP on master:"));
    }

    [Test]
    public async Task GetHeadReflog()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var headRef = await repository.GetCurrentHeadReferenceAsync();
        var reflogs = await repository.GetRelatedReflogsAsync(headRef!.Value);

        Assert.That(reflogs, Is.Not.Null);
        Assert.That(reflogs.Count, Is.GreaterThan(0));
        
        var orderedReflogs = reflogs.OrderByDescending(reflog => reflog.Committer.Date).ToArray();
        Assert.That(orderedReflogs[0].Committer.Name, Is.Not.Empty);
        Assert.That(orderedReflogs[0].Message, Is.Not.Empty);
        Assert.That(orderedReflogs.All(r => r.Current.ToString().Length == 40), Is.True); // すべてSHA-1ハッシュ
    }

    [Test]
    public async Task OpenRawObjectStream()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        using var result = await repository.OpenRawObjectStreamAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var body = await new StreamReader(
            result.Stream, Utilities.UTF8, true)
            .ReadToEndAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Type, Is.EqualTo(ObjectTypes.Commit));
        Assert.That(body, Is.Not.Null);
        Assert.That(body, Does.StartWith("tree 5462bf28fdc4681762057cac7704730b1c590b38"));
        Assert.That(body, Does.Contain("author Kouji Matsui"));
        Assert.That(body, Does.Contain("committer Kouji Matsui"));
    }
}
