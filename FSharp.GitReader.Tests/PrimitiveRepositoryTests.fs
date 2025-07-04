﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open GitReader
open GitReader.Primitive
open NUnit.Framework
open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks

type public PrimitiveRepositoryTests() =

    [<Test>]
    member _.GetCommitDirectly() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        Assert.That(commit.Hash.ToString(), Is.EqualTo("1205dc34ce48bda28fc543daaf9525a9bb6e6d10"))
        Assert.That(commit.TreeRoot.ToString(), Is.EqualTo("5462bf28fdc4681762057cac7704730b1c590b38"))
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Author.MailAddress, Is.EqualTo("k@kekyo.net"))
        Assert.That(commit.Committer.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Message, Does.StartWith("Merge branch 'devel'"))
    }

    [<Test>]
    member _.CommitNotFound() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "0000000000000000000000000000000000000000") |> unwrapOptionAsy
        Assert.That(commit.Hash.ToString(), Is.EqualTo("0000000000000000000000000000000000000000"))
    }

    [<Test>]
    member _.GetCurrentHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! headref = repository.getCurrentHeadReference() |> unwrapOptionAsy
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        Assert.That(commit.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Committer.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Message, Does.StartWith("Added installation .NET 6 SDK on GitHub Actions"))
    }

    [<Test>]
    member _.GetBranchHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! headref = repository.getBranchHeadReference("master")
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        Assert.That(commit.Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Committer.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Message, Does.StartWith("Added installation .NET 6 SDK on GitHub Actions"))
    }

    [<Test>]
    member _.GetRemoteBranchHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! headref = repository.getBranchHeadReference("origin/devel")
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        Assert.That(commit.Hash.ToString(), Is.EqualTo("f2f51b6fe6076ca630ca66c5c9f451217762652a"))
        Assert.That(commit.Author.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Committer.Name, Is.EqualTo("Kouji Matsui"))
        Assert.That(commit.Message, Does.StartWith("Updates test nuget refs."))
    }

    [<Test>]
    member _.GetTag() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! tagref = repository.getTagReference("2.0.0")
        let! tag = repository.getTag(tagref)
        Assert.That(tag.Name, Is.EqualTo("2.0.0"))
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit))
        Assert.That(tag.Hash.ToString(), Is.EqualTo("f64de5e3ad34528757207109e68f626bf8cc1a31"))
        Assert.That(tag.Tagger.HasValue, Is.False)
    }

    [<Test>]
    member _.GetTag2() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! tagref = repository.getTagReference("0.9.6")
        let! tag = repository.getTag(tagref)
        Assert.That(tag.Name, Is.EqualTo("0.9.6"))
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit))
        Assert.That(tag.Hash.ToString(), Is.EqualTo("a7187601f4b4b9dacc3c78895397bb2911d190d6"))
        Assert.That(tag.Tagger.HasValue, Is.True)
        Assert.That(tag.Tagger.Value.Name, Is.EqualTo("Kouji Matsui"))
    }

    [<Test>]
    member _.GetBranchHeads() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! branchrefs = repository.getBranchHeadReferences()
        Assert.That(branchrefs.Length, Is.EqualTo(1))
        Assert.That(branchrefs.[0].Name, Is.EqualTo("master"))
        Assert.That(branchrefs.[0].RelativePath, Is.EqualTo("refs/heads/master"))
        Assert.That(branchrefs.[0].Target.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
    }

    [<Test>]
    member _.GetRemoteBranchHeads() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! branchrefs = repository.getRemoteBranchHeadReferences()
        Assert.That(branchrefs.Length, Is.EqualTo(7))
        let sorted = branchrefs |> Array.sortBy(fun br -> br.Name)
        Assert.That(sorted.[0].Name, Is.EqualTo("origin/HEAD"))
        Assert.That(sorted.[1].Name, Is.EqualTo("origin/appveyor"))
        Assert.That(sorted.[2].Name, Is.EqualTo("origin/devel"))
        Assert.That(sorted.[2].Target.ToString(), Is.EqualTo("f2f51b6fe6076ca630ca66c5c9f451217762652a"))
        Assert.That(sorted.[6].Name, Is.EqualTo("origin/netcore"))
    }
    
    [<Test>]
    member _.GetTags() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! tagrefs = repository.getTagReferences()
        let! tags = Task.WhenAll(
            tagrefs |>
            Seq.map (fun tagReference -> repository.getTag(tagReference) |> Async.StartImmediateAsTask))
        Assert.That(tags.Length, Is.EqualTo(15))
        let sorted = tags |> Array.sortBy(fun t -> t.Name)
        Assert.That(sorted.[0].Name, Is.EqualTo("0.9.5"))
        Assert.That(sorted.[14].Name, Is.EqualTo("2.2.0"))
        Assert.That(sorted.[14].Hash.ToString(), Is.EqualTo("63a8f2c84a8c1b2cf6eabd3e1bd7f1971b912a91"))
    }
     
    [<Test>]
    member _.GetTree() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = repository.getTree(commit.TreeRoot);
        Assert.That(tree.Hash.ToString(), Is.EqualTo("5462bf28fdc4681762057cac7704730b1c590b38"))
        Assert.That(tree.Children.Count, Is.EqualTo(8))
        Assert.That(tree.Children |> Seq.exists (fun c -> c.Name = ".github"), Is.True)
        Assert.That(tree.Children |> Seq.exists (fun c -> c.Name = "build-nupkg.bat"), Is.True)
        Assert.That(tree.Children |> Seq.exists (fun c -> c.Name = "README.md"), Is.True)
    }
     
    [<Test>]
    member _.OpenBlob() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = repository.getTree(commit.TreeRoot);
        let blob = tree.Children |> Seq.find (fun child -> child.Name = "build-nupkg.bat")
        use! blobStream = repository.openBlob blob.Hash
        let tr = new StreamReader(blobStream)
        let content = tr.ReadToEnd()
        Assert.That(content, Does.StartWith("@echo off"))
        Assert.That(content, Does.Contain("dotnet pack"))
    }

    [<Test>]
    member _.TraverseBranchCommits() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! branchref = repository.getBranchHeadReference("master")
        let! commit = repository.getCommit(branchref) |> unwrapOptionAsy
        let mutable c = commit
        let mutable exit = false
        let commits = new List<PrimitiveCommit>();
        while not exit do
            commits.Add(c)
            // Bottom of branch.
            if c.Parents.Count = 0 then
                exit <- true
            else
                // Get primary parent.
                let primary = c.Parents.[0]
                let! commit = repository.getCommit(primary) |> unwrapOptionAsy
                c <- commit
        Assert.That(commits.Count, Is.GreaterThan(5))
        Assert.That(commits.[0].Hash.ToString(), Is.EqualTo("9bb78d13405cab568d3e213130f31beda1ce21d1"))
        Assert.That(commits.[0].Author.Name, Is.EqualTo("Kouji Matsui"))
    }
        
    [<Test>]
    member _.GetStashes() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! stashes = repository.getStashes()
        Assert.That(stashes.Length, Is.EqualTo(2))
        let sorted = stashes |> Array.sortByDescending(fun stash -> stash.Committer.Date)
        Assert.That(sorted.[0].Committer.Name, Is.EqualTo("Julien Richard"))
        Assert.That(sorted.[0].Message, Is.EqualTo("On master: Stash with custom message"))
        Assert.That(sorted.[1].Message, Does.StartWith("WIP on master:"))
    }
    
    [<Test>]
    member _.GetHeadReflogs() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! headRef = repository.getCurrentHeadReference()
        let! reflogs = repository.getRelatedReflogs(headRef.Value)
        Assert.That(reflogs.Length, Is.GreaterThan(0))
        let sorted = reflogs |> Array.sortByDescending(fun reflog -> reflog.Committer.Date)
        Assert.That(sorted.[0].Committer.Name, Is.Not.Empty)
        Assert.That(sorted.[0].Message, Is.Not.Empty)
        Assert.That(sorted |> Array.forall (fun r -> r.Current.ToString().Length = 40), Is.True)
    }
    
    [<Test>]
    member _.OpenRawObjectStream() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"));
        use! result = repository.openRawObjectStream(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10")
        let tr = new StreamReader(result.Stream, Internal.Utilities.UTF8, true)
        let! body = tr.ReadToEndAsync()
        Assert.That(result.Type, Is.EqualTo(ObjectTypes.Commit))
        Assert.That(body, Does.StartWith("tree 5462bf28fdc4681762057cac7704730b1c590b38"))
    }

    [<Test>]
    member _.GetRelatedBranchHeadReferences() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let commitHash = Hash.Parse("f690f0e7bf703582a1fad7e6f1c2d1586390f43d")
        let! relatedBranches = repository.getRelatedBranchHeadReferences(commitHash)
        
        Assert.That(relatedBranches, Is.Not.Null)
        Assert.That(relatedBranches.Length, Is.GreaterThan(0))
        
        // Verify that all returned branches point to the specified commit
        Assert.That(relatedBranches |> Array.forall (fun branch -> branch.Target.Equals(commitHash)), Is.True)
        
        // This specific commit should have at least remote branches
        let remoteBranches = relatedBranches |> Array.filter (fun br -> br.RelativePath.StartsWith("refs/remotes/"))
        Assert.That(remoteBranches.Length, Is.GreaterThan(0), "Should have remote branches")
        
        // Verify branch names are not empty
        Assert.That(relatedBranches |> Array.forall (fun br -> br.Name.Length > 0), Is.True)
    }

    [<Test>]
    member _.GetRelatedTagReferences() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let commitHash = Hash.Parse("30aaea993cc0a3cb1dad2968d3e5f4d90a287e25")
        let! relatedTags = repository.getRelatedTagReferences(commitHash)
        
        Assert.That(relatedTags, Is.Not.Null)
        Assert.That(relatedTags.Length, Is.EqualTo(1), "Should have exactly 1 tag pointing to this commit")
        
        // Verify the specific tag found
        Assert.That(relatedTags.[0].Name, Is.EqualTo("2.1.0"))
        Assert.That(relatedTags.[0].RelativePath, Is.EqualTo("refs/tags/2.1.0"))
        
        // Verify that each tag actually points to the correct commit
        let! primitiveTag = repository.getTag(relatedTags.[0])
        Assert.That(primitiveTag.Hash, Is.EqualTo(commitHash), "Tag should point to the expected commit")
    }

    [<Test>]
    member _.GetRelatedTags() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let commitHash = Hash.Parse("30aaea993cc0a3cb1dad2968d3e5f4d90a287e25")
        let! relatedTags = repository.getRelatedTags(commitHash)
        
        Assert.That(relatedTags, Is.Not.Null)
        Assert.That(relatedTags.Length, Is.EqualTo(1), "Should have exactly 1 tag pointing to this commit")
        
        // Verify the specific tag found
        let tag = relatedTags.[0]
        Assert.That(tag.Hash, Is.EqualTo(commitHash), "Tag should point to the expected commit")
        Assert.That(tag.Name, Is.EqualTo("2.1.0"))
        Assert.That(tag.Type, Is.EqualTo(ObjectTypes.Commit))
    }

    [<Test>]
    member _.GetRelatedTags_CompareWithGetRelatedTagReferences() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let commitHash = Hash.Parse("30aaea993cc0a3cb1dad2968d3e5f4d90a287e25")
        
        // Get tags using both methods
        let! tagReferences = repository.getRelatedTagReferences(commitHash)
        let! tags = repository.getRelatedTags(commitHash)
        
        Assert.That(tags.Length, Is.EqualTo(tagReferences.Length), 
                   "Both methods should return the same number of results")
        
        // Verify that each tag corresponds to the correct tag reference
        for i in 0 .. tags.Length - 1 do
            let tag = tags.[i]
            let tagRef = tagReferences |> Array.find (fun tr -> tr.Name = tag.Name)
            
            Assert.That(tagRef, Is.Not.Null, sprintf "Tag reference for '%s' should exist" tag.Name)
            Assert.That(tag.Hash, Is.EqualTo(commitHash), sprintf "Tag '%s' should point to the expected commit" tag.Name)
    }

    [<Test>]
    member _.CrackMessage_SimpleMessage() = task {
        // Use existing test repository for F# test
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "9bb78d13405cab568d3e213130f31beda1ce21d1") |> unwrapOptionAsy
        
        let (subject, body) = commit.crackMessage()
        
        Assert.That(subject, Is.EqualTo("Added installation .NET 6 SDK on GitHub Actions."))
        Assert.That(body, Is.Empty) // This commit has no body
    }

    [<Test>]
    member _.CrackMessage_SingleLineMessage() = task {
        // This test uses the existing test1 repository which has single-line commits
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.getBasePath("test1"))
        let! commit = repository.getCommit(
            "9bb78d13405cab568d3e213130f31beda1ce21d1") |> unwrapOptionAsy
        
        let (subject, body) = commit.crackMessage()
        
        Assert.That(subject, Is.EqualTo("Added installation .NET 6 SDK on GitHub Actions."))
        Assert.That(body, Is.Empty)
    }

    [<Test>]
    member _.ToGitRawDateString_PositiveOffset() =
        let date = DateTimeOffset(2023, 7, 15, 14, 30, 45, TimeSpan.FromHours(9.0))
        let rawString = date.toGitRawDateString()
        
        // Use the actual computed Unix timestamp
        let expectedUnixTime = date.ToUnixTimeSeconds()
        Assert.That(rawString, Is.EqualTo($"{expectedUnixTime} +0900"))

    [<Test>]
    member _.ToGitRawDateString_NegativeOffset() =
        let date = DateTimeOffset(2023, 7, 15, 14, 30, 45, TimeSpan.FromHours(-5.0))
        let rawString = date.toGitRawDateString()
        
        // Use the actual computed Unix timestamp
        let expectedUnixTime = date.ToUnixTimeSeconds()
        Assert.That(rawString, Is.EqualTo($"{expectedUnixTime} -0500"))

    [<Test>]
    member _.ToGitRawDateString_ZeroOffset() =
        let date = DateTimeOffset(2023, 7, 15, 14, 30, 45, TimeSpan.Zero)
        let rawString = date.toGitRawDateString()
        
        // Use the actual computed Unix timestamp
        let expectedUnixTime = date.ToUnixTimeSeconds()
        Assert.That(rawString, Is.EqualTo($"{expectedUnixTime} +0000"))
