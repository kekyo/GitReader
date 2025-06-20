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
using System.Threading.Tasks;

namespace GitReader.Structures;

public sealed class RepositoryTests
{
    [Test]
    public async Task GetCurrentHead()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        Assert.That(repository.Head, Is.Not.Null);
        Assert.That(repository.Head!.Name, Is.EqualTo("master"));
        Assert.That(repository.Head.IsRemote, Is.False);
    }

    [Test]
    public async Task GetCommitDirectly()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        Assert.That(commit, Is.Not.Null);
        Assert.That(commit!.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"));
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Author.MailAddress, Is.EqualTo("k@kekyo.net"));
        Assert.That(commit.Committer.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(commit.Subject, Is.EqualTo("Merge branch 'devel'"));
        Assert.That(commit.Body, Is.Empty);
        Assert.That(commit.Tags.Count, Is.EqualTo(1));
        Assert.That(commit.Tags[0].Name, Is.EqualTo("2.0.18"));
    }

    [Test]
    public async Task CommitNotFound()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "0000000000000000000000000000000000000000");

        Assert.That(commit, Is.Null);
    }

    [Test]
    public async Task GetBranch()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branch = repository.Branches["master"];
        var headCommit = await branch.GetHeadCommitAsync();

        Assert.That(branch, Is.Not.Null);
        Assert.That(branch.Name, Is.EqualTo("master"));
        Assert.That(headCommit, Is.Not.Null);
        Assert.That(headCommit.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"));
        Assert.That(headCommit.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(headCommit.Subject, Is.EqualTo("Added installation .NET 6 SDK on GitHub Actions."));
    }

    [Test]
    public async Task GetRemoteBranch()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branch = repository.Branches["origin/devel"];

        Assert.That(branch, Is.Not.Null);
        Assert.That(branch.Name, Is.EqualTo("origin/devel"));
        Assert.That(branch.IsRemote, Is.True);
    }

    [Test]
    public async Task GetTag()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tag = repository.Tags["2.0.0"];
        var commit = await tag.GetCommitAsync();

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Name, Is.EqualTo("2.0.0"));
        Assert.That(commit, Is.Not.Null);
        Assert.That(commit.Hash.ToString(), Is.EqualTo("f64de5e3ad34528757207109e68f626bf8cc1a31"));
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"));
    }

    [Test]
    public async Task GetTag2()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tag = repository.Tags["0.9.6"];
        var commit = await tag.GetCommitAsync();

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Name, Is.EqualTo("0.9.6"));
        Assert.That(commit, Is.Not.Null);
        Assert.That(commit.Hash.ToString(), Is.EqualTo("a7187601f4b4b9dacc3c78895397bb2911d190d6"));
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"));
    }

    [Test]
    public async Task GetAnnotation()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var tag = repository.Tags["0.9.6"];
        var annotation = await tag.GetAnnotationAsync();

        Assert.That(tag, Is.Not.Null);
        Assert.That(tag.Name, Is.EqualTo("0.9.6"));
        Assert.That(annotation, Is.Not.Null);
        Assert.That(annotation!.Tagger, Is.Not.Null);
        Assert.That(annotation.Tagger!.Value.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(annotation.Message, Is.Not.Null);
    }

    [Test]
    public async Task GetBranchesFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "9bb78d13405cab568d3e213130f31beda1ce21d1");
        var branches = commit!.Branches;

        Assert.That(commit, Is.Not.Null);
        Assert.That(branches, Is.Not.Null);
        Assert.That(branches.Count, Is.GreaterThan(0));
        
        var orderedBranches = branches.OrderBy(b => b.Name).ToArray();
        Assert.That(orderedBranches.Any(b => b.Name == "master"), Is.True);
        Assert.That(orderedBranches.All(b => b.Name.Length > 0), Is.True);
    }

    [Test]
    public async Task GetTagsFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "f64de5e3ad34528757207109e68f626bf8cc1a31");
        var tags = commit!.Tags;

        Assert.That(commit, Is.Not.Null);
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Count, Is.GreaterThan(0));
        
        var orderedTags = tags.OrderBy(t => t.Name).ToArray();
        Assert.That(orderedTags.Any(t => t.Name == "2.0.0"), Is.True);
        Assert.That(orderedTags.All(t => t.Name.Length > 0), Is.True);
    }

    [Test]
    public async Task GetRemoteBranchesFromCommit()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "f690f0e7bf703582a1fad7e6f1c2d1586390f43d");
        var branches = commit!.Branches;

        Assert.That(commit, Is.Not.Null);
        Assert.That(branches, Is.Not.Null);
        Assert.That(branches.Count, Is.GreaterThan(0));
        
        var orderedBranches = branches.OrderBy(br => br.Name).ToArray();
        Assert.That(orderedBranches.Any(br => br.IsRemote), Is.True);
        Assert.That(orderedBranches.All(br => br.Name.Length > 0), Is.True);
    }

    [Test]
    public async Task GetParentCommits()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "dc45301eeb49ec94ee043755124802497d0079ec");
        var parents = await commit!.GetParentCommitsAsync();

        Assert.That(commit, Is.Not.Null);
        Assert.That(parents, Is.Not.Null);
        Assert.That(parents.Count, Is.GreaterThan(0));
        Assert.That(parents.All(p => p.Hash.ToString().Length == 40), Is.True);
        Assert.That(parents.All(p => p.Author.Name.Length > 0), Is.True);
    }

    [Test]
    public async Task GetTreeRoot()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var treeRoot = await commit!.GetTreeRootAsync();

        Assert.That(commit, Is.Not.Null);
        Assert.That(treeRoot, Is.Not.Null);
        Assert.That(treeRoot.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"));
        Assert.That(treeRoot.Children, Is.Not.Null);
        Assert.That(treeRoot.Children.Count, Is.GreaterThan(5));
        Assert.That(treeRoot.Children.Any(c => c.Name == "README.md"), Is.True);
    }

    [Test]
    public async Task OpenBlob()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var commit = await repository.GetCommitAsync(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10");

        var tree = await commit!.GetTreeRootAsync();

        var blob = tree.Children.OfType<TreeBlobEntry>().
            First(child => child.Name == "build-nupkg.bat");

        using var blobStream = await blob.OpenBlobAsync();
        var blobText = new StreamReader(blobStream).ReadToEnd();

        Assert.That(blobText, Is.Not.Null);
        Assert.That(blobText, Does.StartWith("@echo off"));
        Assert.That(blobText, Does.Contain("rem CenterCLR.NamingFormatter"));
        Assert.That(blobText, Does.Contain("msbuild"));
        Assert.That(blobText.Length, Is.GreaterThan(500));
    }

    [Test]
    public async Task OpenSubModule()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test4"));

        var commit = await repository.GetCommitAsync(
            "37021d38937107d5782f063f78f502f2da14c751");

        var tree = await commit!.GetTreeRootAsync();

        var subModule = tree.Children.OfType<TreeSubModuleEntry>().
            First(child => child.Name == "GitReader");

        using var subModuleRepository = await subModule.OpenSubModuleAsync();

        var subModuleCommit = await subModuleRepository.GetCommitAsync(
            "ce68b633419a8b16d642e6ea1ec3492cdbdf2584");

        Assert.That(subModuleCommit, Is.Not.Null);
        Assert.That(subModuleCommit!.Hash.ToString(), Is.EqualTo("ce68b633419a8b16d642e6ea1ec3492cdbdf2584"));
        Assert.That(subModuleCommit.Author.Name, Is.EqualTo("Kouji Matsui"));
        Assert.That(subModuleCommit.Subject, Does.StartWith("Merge"));
        Assert.That(subModuleCommit.Tags.Count, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task TraverseBranchCommits()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        var branch = repository.Branches["master"];

        var commits = new List<Commit>();

        Commit? current = await branch.GetHeadCommitAsync();
        while (current != null)
        {
            commits.Add(current);

            // Get primary parent commit.
            current = await current.GetPrimaryParentCommitAsync();
        }

        Assert.That(commits, Is.Not.Null);
        Assert.That(commits.Count, Is.GreaterThan(5));
        Assert.That(commits[0].Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"));
        Assert.That(commits.All(c => c.Author.Name == "Kouji Matsui"), Is.True);
        Assert.That(commits.All(c => c.Hash.ToString().Length == 40), Is.True);
    }

    [Test]
    public async Task GetRemoteUrls()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
            RepositoryTestsSetUp.GetBasePath("test1"));

        Assert.That(repository.RemoteUrls, Is.Not.Null);
        Assert.That(repository.RemoteUrls.Count, Is.EqualTo(1));
        Assert.That(repository.RemoteUrls.ContainsKey("origin"), Is.True);
        Assert.That(repository.RemoteUrls["origin"], Is.EqualTo("https://github.com/kekyo/CenterCLR.NamingFormatter"));
    }

    [Test]
    public async Task GetRemoteUrls2()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(
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
        using var repository = await Repository.Factory.OpenStructureAsync(RepositoryTestsSetUp.GetBasePath("test1"));

        var stashes = await Task.WhenAll(
            repository.Stashes.
            OrderByDescending(stash => stash.Committer.Date).
            Select(async stash => new { Stash = stash, Commit = await stash.GetCommitAsync(), }));

        Assert.That(stashes, Is.Not.Null);
        Assert.That(stashes.Length, Is.GreaterThanOrEqualTo(0));
        if (stashes.Length > 0)
        {
            Assert.That(stashes.All(s => s.Stash.Committer.Name.Length > 0), Is.True);
            Assert.That(stashes.All(s => s.Commit != null), Is.True);
            Assert.That(stashes.All(s => s.Commit!.Hash.ToString().Length == 40), Is.True);
        }
    }

    [Test]
    public async Task GetHeadReflog()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(RepositoryTestsSetUp.GetBasePath("test1"));

        var reflogs = await repository.GetHeadReflogsAsync();

        var results = await Task.WhenAll(reflogs.
            OrderByDescending(reflog => reflog.Committer.Date).
            Select(async reflog =>
            {
                var current = await reflog.GetCurrentCommitAsync();
                var old = await reflog.GetOldCommitAsync();
                return new { Commit = current, OldCommit = old, Reflog = reflog, };
            }));

        Assert.That(reflogs, Is.Not.Null);
        Assert.That(results, Is.Not.Null);
        Assert.That(results.Length, Is.GreaterThan(0));
        Assert.That(results.All(r => r.Commit != null), Is.True);
        Assert.That(results.All(r => r.Reflog.Committer.Name.Length > 0), Is.True);
        Assert.That(results.All(r => r.Reflog.Message.Length > 0), Is.True);
    }

    [Test]
    public async Task CacheFileStreamDisposeProperly()
    {
        var repositoryPath = RepositoryTestsSetUp.GetBasePath("test2");

        using (var repository = await Repository.Factory.OpenStructureAsync(repositoryPath))
        {
            // simulate work with repository

            Assert.That(repository.Head, Is.Not.Null);
            Assert.That(repository.Head!.Name, Is.EqualTo("main"));
        }

        var path = Path.Combine(repositoryPath, ".git", "refs", "heads", "main");

        // simulate commiting to main branch
        var fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 10, true);

        fileStream.Dispose();
    }
}
