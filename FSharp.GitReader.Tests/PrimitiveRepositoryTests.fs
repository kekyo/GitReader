////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open GitReader
open GitReader.Primitive
open NUnit.Framework
open System.Collections.Generic
open System.Threading.Tasks
open System.IO

type public PrimitiveRepositoryTests() =

    [<Test>]
    member _.GetCommitDirectly() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.CommitNotFound() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "0000000000000000000000000000000000000000") |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.GetCurrentHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! headref = repository.getCurrentHeadReference() |> unwrapOptionAsy
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.GetBranchHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! headref = repository.getBranchHeadReference("master")
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.GetRemoteBranchHead() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! headref = repository.getRemoteBranchHeadReference("origin/devel")
        let! commit = repository.getCommit(headref) |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.GetTag() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! tagref = repository.getTagReference("2.0.0")
        let! tag = repository.getTag(tagref)
        do! verify(tag)
    }

    [<Test>]
    member _.GetTag2() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! tagref = repository.getTagReference("0.9.6")
        let! tag = repository.getTag(tagref)
        do! verify(tag)
    }

    [<Test>]
    member _.GetBranchHeads() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! branchrefs = repository.getBranchHeadReferences()
        do! verify(branchrefs |> Array.sortBy(fun br -> br.Name))
    }

    [<Test>]
    member _.GetRemoteBranchHeads() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! branchrefs = repository.getRemoteBranchHeadReferences()
        do! verify(branchrefs |> Array.sortBy(fun br -> br.Name))
    }
    
    [<Test>]
    member _.GetTags() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! tagrefs = repository.getTagReferences()
        let! tags = Task.WhenAll(
            tagrefs |>
            Seq.map (fun tagReference -> repository.getTag(tagReference) |> Async.StartImmediateAsTask))
        do! verify(tags |> Array.sortBy(fun t -> t.Name))
    }
     
    [<Test>]
    member _.GetTree() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = repository.getTree(commit.TreeRoot);
        do! verify(tree)
    }
     
    [<Test>]
    member _.OpenBlob() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = repository.getTree(commit.TreeRoot);
        let blob = tree.Children |> Seq.find (fun child -> child.Name = "build-nupkg.bat")
        use! blobStream = repository.openBlob blob.Hash
        let tr = new StreamReader(blobStream)
        do! verify(tr.ReadToEnd())
    }

    [<Test>]
    member _.TraverseBranchCommits() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! branchref = repository.getBranchHeadReference("master")
        let! commit = repository.getCommit(branchref) |> unwrapOptionAsy
        let mutable c = commit
        let mutable exit = false
        let commits = new List<PrimitiveCommit>();
        while not exit do
            commits.Add(c)
            // Bottom of branch.
            if c.Parents.Length = 0 then
                exit <- true
            else
                // Get primary parent.
                let primary = c.Parents.[0]
                let! commit = repository.getCommit(primary) |> unwrapOptionAsy
                c <- commit
        return! verify(commits.ToArray())
    }
        
    [<Test>]
    member _.GetStashes() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! stashes = repository.getStashes()
        return! verify(stashes)
    }
    
    [<Test>]
    member _.GetHeadRefLog() = task {
        use! repository = Repository.Factory.openPrimitive(
            RepositoryTestsSetUp.BasePath)
        let! headRef = repository.getCurrentHeadReference()
        let! reflog = repository.getRefLog(headRef.Value)
        return! verify(reflog)
    }
