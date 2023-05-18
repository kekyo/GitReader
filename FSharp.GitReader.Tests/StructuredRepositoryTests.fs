////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
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

type public StructuredRepositoryTests() =

    [<Test>]
    member _.GetCurrentHead() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let head = repository.getCurrentHead() |> unwrapOption
        do! verify(head)
    }

    [<Test>]
    member _.GetCommitDirectly() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        do! verify(commit)
    }

    [<Test>]
    member _.CommitNotFound() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "0000000000000000000000000000000000000000")
        do! verify(commit)
    }

    [<Test>]
    member _.GetBranch() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let branch = repository.Branches["master"]
        do! verify(branch)
    }

    [<Test>]
    member _.GetRemoteBranch() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let branch = repository.RemoteBranches["origin/devel"]
        do! verify(branch)
    }

    [<Test>]
    member _.GetTag() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let tag = repository.Tags["2.0.0"]
        do! verify(tag)
    }

    [<Test>]
    member _.GetTag2() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let tag = repository.Tags["0.9.6"]
        do! verify(tag)
    }

    [<Test>]
    member _.GetBranchesFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "9bb78d13405cab568d3e213130f31beda1ce21d1") |> unwrapOptionAsy
        let branches = commit.Branches
        do! verify(branches |> Seq.sortBy(fun br -> br.Name)
            // HACK: Fixed by Array.toList, because avoid serialization in BranchArrayConverter.
            |> Seq.toList)
    }

    [<Test>]
    member _.GetTagsFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "f64de5e3ad34528757207109e68f626bf8cc1a31") |> unwrapOptionAsy
        let tags = commit.Tags
        do! verify(tags |> Seq.sortBy(fun t -> t.Name))
    }

    [<Test>]
    member _.GetRemoteBranchesFromCommit() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "f690f0e7bf703582a1fad7e6f1c2d1586390f43d") |> unwrapOptionAsy
        let branches = commit.RemoteBranches
        do! verify(branches |> Seq.sortBy(fun br -> br.Name)
            // HACK: Fixed by Array.toList, because avoid serialization in BranchArrayConverter.
            |> Seq.toList)
    }

    [<Test>]
    member _.GetParentCommits() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "dc45301eeb49ec94ee043755124802497d0079ec") |> unwrapOptionAsy
        let! parents = commit.getParentCommits()
        do! verify(parents)
    }
   
    [<Test>]
    member _.GetTreeRoot() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! treeRoot = commit.getTreeRoot()
        do! verify(treeRoot)
    }
         
    [<Test>]
    member _.OpenBlob() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let! commit = repository.getCommit(
            "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") |> unwrapOptionAsy
        let! tree = commit.getTreeRoot()
        let blob = tree.Children |> Seq.find (fun child -> child.Name = "build-nupkg.bat") :?> TreeBlobEntry
        use! blobStream = blob.openBlob()
        let tr = new StreamReader(blobStream)
        do! verify(tr.ReadToEnd())
    }

    [<Test>]
    member _.TraverseBranchCommits() = task {
        use! repository = Repository.Factory.openStructured(
            RepositoryTestsSetUp.BasePath)
        let branch = repository.Branches["master"]
        let commits = new List<Commit>()
        let mutable current = branch.Head
        while not (isNull current) do
            commits.Add(current)
            // Get primary parent commit.
            let! c = current.getPrimaryParentCommit() |> unwrapOptionAsy
            current <- c
        do! verify(commits.ToArray())
    }
