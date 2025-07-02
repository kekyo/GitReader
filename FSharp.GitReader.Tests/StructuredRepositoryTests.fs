////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open GitReader.Structures
open NUnit.Framework
open System.Collections.Generic
open System.IO
open System.Threading.Tasks

type public StructuredRepositoryTests() =

    [<Test>]
    member _.GetCurrentHead() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let headBranch = repository.getCurrentHead() |> unwrapOption
        let! head = headBranch.getHeadCommit()
        Assert.That(head.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(head.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(head.Subject, Does.StartWith("Added installation .NET 6 SDK on GitHub Actions"))
    }

    [<Test>]
    member _.GetCommitDirectly() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! treeRoot = commit.getTreeRoot()
        Assert.That(commit.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"))
        Assert.That(treeRoot, Is.Not.Null)
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Subject, Does.StartWith("Merge branch 'devel'"))
    }

    [<Test>]
    member _.CommitNotFound() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "0000000000000000000000000000000000000000") |> unwrapOptionAsy
        Assert.That(commit, Is.Null)
    }

    [<Test>]
    member _.GetBranch() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let branch = repository.Branches["master"]
        Assert.That(branch.Name, Is.EqualTo("master"))
        Assert.That(branch.Head.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(branch.IsRemote, Is.False)
    }

    [<Test>]
    member _.GetRemoteBranch() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let branch = repository.Branches["origin/devel"]
        Assert.That(branch.Name, Is.EqualTo("origin/devel"))
        Assert.That(branch.Head.ToString(), Is.EqualTo("f2f51b6fe6076ca630ca66c5c9f451217762652a"))
        Assert.That(branch.IsRemote, Is.True)
    }

    [<Test>]
    member _.GetTag() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let tag = repository.Tags["2.0.0"]
        let! commit = tag.getCommit()
        Assert.That(tag.Name, Is.EqualTo("2.0.0"))
        Assert.That(commit.Hash.ToString(), Is.EqualTo("f64de5e3ad34528757207109e68f626bf8cc1a31"))
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit))
    }

    [<Test>]
    member _.GetTag2() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let tag = repository.Tags["0.9.6"]
        let! commit = tag.getCommit()
        Assert.That(tag.Name, Is.EqualTo("0.9.6"))
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit))
        Assert.That(commit.Hash.ToString(), Is.EqualTo("a7187601f4b4b9dacc3c78895397bb2911d190d6"))
    }

    [<Test>]
    member _.GetAnnotation() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let tag = repository.Tags["0.9.6"]
        let! annotation = tag.getAnnotation()
        Assert.That(tag.Name, Is.EqualTo("0.9.6"))
        Assert.That(annotation, Is.Not.Null)
        Assert.That(annotation.Message, Is.EqualTo(""))
    }

    [<Test>]
    member _.GetBranchesFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "9bb78d13405cab568d3e213130f31beda1ce21d1") |> unwrapOptionAsy
        let! results = Task.WhenAll(
            commit.Branches
            |> Seq.sortBy(fun br -> br.Name)
            |> Seq.map(fun br -> task {
                let! head = br.getHeadCommit()
                return (br.Name, head) }))
        Assert.That(results.Length, Is.GreaterThan(0))
        let branchNames = results |> Array.map fst
        Assert.That(branchNames |> Array.exists (fun n -> n = "master"), Is.True)
        Assert.That(branchNames |> Array.forall (fun n -> n.Length > 0), Is.True)
    }

    [<Test>]
    member _.GetTagsFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "f64de5e3ad34528757207109e68f626bf8cc1a31") |> unwrapOptionAsy
        let tags = commit.Tags
        let sorted = tags |> Seq.sortBy(fun t -> t.Name) |> Seq.toArray
        Assert.That(sorted.Length, Is.GreaterThan(0))
        Assert.That(sorted |> Array.exists (fun t -> t.Name = "2.0.0"), Is.True)
        Assert.That(sorted |> Array.forall (fun t -> t.Name.Length > 0), Is.True)
    }

    [<Test>]
    member _.GetRemoteBranchesFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "f2f51b6fe6076ca630ca66c5c9f451217762652a") |> unwrapOptionAsy
        let! results = Task.WhenAll(
            commit.Branches
            |> Seq.sortBy(fun br -> br.Name)
            |> Seq.map(fun br -> task {
                let! head = br.getHeadCommit()
                return (br.Name, head) }))
        Assert.That(results.Length, Is.GreaterThan(0))
        let branchNames = results |> Array.map fst
        Assert.That(branchNames |> Array.exists (fun n -> n.StartsWith("origin/")), Is.True)
        Assert.That(branchNames |> Array.forall (fun n -> n.Length > 0), Is.True)
    }

    [<Test>]
    member _.GetParentCommits() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "dc45301eeb49ec94ee043755124802497d0079ec") |> unwrapOptionAsy
        let! parents = commit.getParentCommits()
        Assert.That(parents.Length, Is.GreaterThan(0))
        Assert.That(parents |> Array.forall (fun p -> p.Hash.ToString().Length = 40), Is.True)
        Assert.That(parents |> Array.forall (fun p -> p.Author.Name.Length > 0), Is.True)
    }
   
    [<Test>]
    member _.GetTreeRoot() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! treeRoot = commit.getTreeRoot()
        Assert.That(treeRoot.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"))
        Assert.That(treeRoot.Children |> Seq.length, Is.EqualTo(8))
        Assert.That(treeRoot.Children |> Seq.exists (fun c -> c.Name = ".github"), Is.True)
        Assert.That(treeRoot.Children |> Seq.exists (fun c -> c.Name = "build-nupkg.bat"), Is.True)
        Assert.That(treeRoot.Children |> Seq.exists (fun c -> c.Name = "README.md"), Is.True)
    }
         
    [<Test>]
    member _.OpenBlob() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = commit.getTreeRoot()
        let blob = tree.Children |> Seq.find (fun child -> child.Name = "build-nupkg.bat") :?> TreeBlobEntry
        use! blobStream = blob.openBlob()
        let tr = new StreamReader(blobStream)
        let content = tr.ReadToEnd()
        Assert.That(content, Does.StartWith("@echo off"))
        Assert.That(content, Does.Contain("dotnet pack"))
    }

    [<Test>]
    member _.TraverseBranchCommits() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let branch = repository.Branches["master"]
        let commits = new List<Commit>()
        let! head = branch.getHeadCommit();
        let mutable current = head
        while not (isNull current) do
            commits.Add(current)
            // Get primary parent commit.
            let! c = current.getPrimaryParentCommit() |> unwrapOptionAsy
            current <- c
        Assert.That(commits.Count, Is.GreaterThan(5))
        Assert.That(commits.[0].Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(commits.[0].Author.Name, Is.EqualTo("Kouji Matsui"))
    }
   
    [<Test>]
    member _.GetRemoteUrls() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        Assert.That(repository.RemoteUrls.Count, Is.EqualTo(1))
        Assert.That(repository.RemoteUrls |> Seq.exists (fun kvp -> kvp.Key = "origin"), Is.True)
        Assert.That(repository.RemoteUrls.["origin"], Does.StartWith("https://github.com/"))
    }

    [<Test>]
    member _.GetStashes() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! results = Task.WhenAll(repository.Stashes
            |> Seq.sortByDescending(fun stash -> stash.Committer.Date)
            |> Seq.map(fun stash -> task {
                let! commit = stash.getCommit()
                return (stash, commit) }))
        Assert.That(results.Length, Is.EqualTo(2))
        Assert.That((results.[0] |> fst).Committer.Name, Is.EqualTo("Julien Richard"))
        Assert.That((results.[0] |> fst).Message, Is.EqualTo("On master: Stash with custom message"))
        Assert.That((results.[1] |> fst).Message, Does.StartWith("WIP on master:"))
    }

    [<Test>]
    member _.GetHeadReflog() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! reflogs = repository.getHeadReflogs()
        let! results = Task.WhenAll(reflogs
            |> Seq.sortByDescending(fun reflog -> reflog.Committer.Date)
            |> Seq.map(fun reflog -> task {
                let! current = reflog.getCurrentCommit()
                let! old = reflog.getOldCommit()
                return (current, old, reflog) }))
        Assert.That(results.Length, Is.GreaterThan(0))
        Assert.That((results.[0] |> fun (_, _, r) -> r).Committer.Name, Is.Not.Empty)
        Assert.That((results.[0] |> fun (_, _, r) -> r).Message, Is.Not.Empty)
        Assert.That(results |> Array.forall (fun (_, _, r) -> r.Commit.ToString().Length = 40), Is.True)
    }

    [<Test>]
    member _.GetMessage() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        
        let message = commit.getMessage()
        
        Assert.That(message, Does.StartWith("Merge branch 'devel'"))
    }

    [<Test>]
    member _.Commit_MessageProperty() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "9bb78d13405cab568d3e213130f31beda1ce21d1") |> unwrapOptionAsy
        
        Assert.That(commit.Subject, Is.EqualTo("Added installation .NET 6 SDK on GitHub Actions."))
        Assert.That(commit.Body, Is.Empty)
    }
