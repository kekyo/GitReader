////////////////////////////////////////////////////////////////////////////
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
open NUnit.Framework.Legacy
open System.IO

type public Primitive_WorktreeTests() =

    [<Test>]
    member _.GetWorktreesMainOnly() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with initial commit
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create initial file and commit
            do! TestUtilities.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository").asAsync()
            do! runGitCommandAsync(testPath, "add README.md")
            do! runGitCommandAsync(testPath, "commit -m \"Initial commit\"")

            use! repository = Repository.Factory.openPrimitive(testPath)

            let! worktrees = repository.getWorktrees()

            // Should have only main worktree
            do ClassicAssert.AreEqual(1, worktrees.Count, "Should have exactly one worktree")
            
            let mainWorktree = worktrees.[0]
            do ClassicAssert.AreEqual("(main)", mainWorktree.Name)
            do ClassicAssert.AreEqual(testPath, mainWorktree.Path)
            do ClassicAssert.AreEqual(WorktreeStatus.Normal, mainWorktree.Status)
            do ClassicAssert.IsTrue(mainWorktree.IsMain)
            
            // Test async methods
            let! head = mainWorktree.getHead()
            let! branch = mainWorktree.getBranch()
            
            do ClassicAssert.IsNotNull(head, "Should have HEAD commit")
            do ClassicAssert.IsNotNull(branch, "Should have current branch")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }

    [<Test>]
    member _.GetWorktreesWithAdditionalWorktrees() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"))
        let worktree1Path = Path.Combine(testPath, "worktree1")
        let worktree2Path = Path.Combine(testPath, "worktree2")
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with initial commit
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create initial file and commit
            do! TestUtilities.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository").asAsync()
            do! runGitCommandAsync(testPath, "add README.md")
            do! runGitCommandAsync(testPath, "commit -m \"Initial commit\"")
            
            // Create branches and worktrees
            do! runGitCommandAsync(testPath, "branch feature1")
            do! runGitCommandAsync(testPath, "branch feature2")
            do! runGitCommandAsync(testPath, $"worktree add \"{worktree1Path}\" feature1")
            do! runGitCommandAsync(testPath, $"worktree add \"{worktree2Path}\" feature2")

            use! repository = Repository.Factory.openPrimitive(testPath)

            let! worktrees = repository.getWorktrees()

            // Should have main + 2 additional worktrees
            do ClassicAssert.AreEqual(3, worktrees.Count, "Should have exactly three worktrees")
            
            // Find main worktree
            let mainWorktree = worktrees |> Seq.find(fun w -> w.IsMain)
            do ClassicAssert.AreEqual("(main)", mainWorktree.Name)
            do ClassicAssert.AreEqual(testPath, mainWorktree.Path)
            do ClassicAssert.AreEqual(WorktreeStatus.Normal, mainWorktree.Status)
            
            // Find feature worktrees and test async methods
            let feature1Worktree = worktrees |> Seq.find(fun w -> w.Name = "worktree1")
            do ClassicAssert.AreEqual(worktree1Path, feature1Worktree.Path)
            do ClassicAssert.AreEqual(WorktreeStatus.Normal, feature1Worktree.Status)
            do ClassicAssert.IsFalse(feature1Worktree.IsMain)
            
            let! feature1Branch = feature1Worktree.getBranch()
            do ClassicAssert.AreEqual("feature1", feature1Branch)
            
            let feature2Worktree = worktrees |> Seq.find(fun w -> w.Name = "worktree2")
            do ClassicAssert.AreEqual(worktree2Path, feature2Worktree.Path)
            do ClassicAssert.AreEqual(WorktreeStatus.Normal, feature2Worktree.Status)
            do ClassicAssert.IsFalse(feature2Worktree.IsMain)
            
            let! feature2Branch = feature2Worktree.getBranch()
            do ClassicAssert.AreEqual("feature2", feature2Branch)
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }

    [<Test>]
    member _.GetWorktreesOpenFromWorktreeDirectory() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"))
        let worktreePath = Path.Combine(testPath, "feature-branch")
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with initial commit
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create initial file and commit
            do! TestUtilities.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository").asAsync()
            do! runGitCommandAsync(testPath, "add README.md")
            do! runGitCommandAsync(testPath, "commit -m \"Initial commit\"")
            
            // Create branch and worktree
            do! runGitCommandAsync(testPath, "branch feature")
            do! runGitCommandAsync(testPath, $"worktree add \"{worktreePath}\" feature")

            // Open repository from worktree directory
            use! repository = Repository.Factory.openPrimitive(worktreePath)

            let! worktrees = repository.getWorktrees()

            // Should have main + 1 additional worktree
            do ClassicAssert.AreEqual(2, worktrees.Count, "Should have exactly two worktrees")
            
            // Check that we can see both worktrees even when opening from worktree
            let mainWorktree = worktrees |> Seq.find(fun w -> w.IsMain)
            do ClassicAssert.AreEqual("(main)", mainWorktree.Name)
            do ClassicAssert.AreEqual(testPath, mainWorktree.Path)
            
            let featureWorktree = worktrees |> Seq.find(fun w -> not w.IsMain)
            do ClassicAssert.AreEqual(worktreePath, featureWorktree.Path)
            do ClassicAssert.IsFalse(featureWorktree.IsMain)
            
            // Test async method for branch
            let! featureBranch = featureWorktree.getBranch()
            do ClassicAssert.AreEqual("feature", featureBranch)
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }
