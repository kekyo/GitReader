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
open System.IO

type public Structured_WorkingDirectoryStatusTests() =

    [<Test>]
    member _.GetWorkingDirectoryStatusEmptyRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create an empty Git repository
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")

            use! repository = Repository.Factory.openStructured(testPath)

            let! status = repository.getWorkingDirectoryStatus()

            // Empty repository should have no staged, unstaged, or untracked files
            do Assert.AreEqual(0, status.StagedFiles.Count, "Empty repository should have no staged files")
            do Assert.AreEqual(0, status.UnstagedFiles.Count, "Empty repository should have no unstaged files")
            do Assert.AreEqual(0, status.UntrackedFiles.Count, "Empty repository should have no untracked files")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }

    [<Test>]
    member _.GetWorkingDirectoryStatusWithModifications() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))

        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with initial commit
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create initial file and commit
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.") |> Async.AwaitTask
            do! runGitCommandAsync(testPath, "add README.md")
            do! runGitCommandAsync(testPath, "commit -m \"Initial commit\"")
            
            // Create new untracked file
            do! File.WriteAllTextAsync(Path.Combine(testPath, "new_file.txt"), "This is a new file for testing.") |> Async.AwaitTask
            
            // Modify existing tracked file
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.\n\nModified for testing.") |> Async.AwaitTask

            use! repository = Repository.Factory.openStructured(testPath)

            let! status = repository.getWorkingDirectoryStatus()

            // Should have untracked files (including new_file.txt)
            do Assert.IsTrue(status.UntrackedFiles.Count > 0, "Should have untracked files")
            
            let newFile = status.UntrackedFiles |> Seq.tryFind(fun f -> f.Path = "new_file.txt")
            match newFile with
            | Some nf ->
                do Assert.AreEqual(FileStatus.Untracked, nf.Status)
                do Assert.IsNull(nf.IndexHash)
                do Assert.IsNotNull(nf.WorkingTreeHash)
            | _ ->
                do Assert.Fail("new_file.txt should be in untracked files")

            // README.md should be modified and appear in unstaged files
            let modifiedFile = status.UnstagedFiles |> Seq.tryFind(fun f -> f.Path = "README.md")
            match modifiedFile with
            | Some mf ->
                do Assert.AreEqual(FileStatus.Modified, mf.Status)
                do Assert.IsNotNull(mf.IndexHash)
                do Assert.IsNotNull(mf.WorkingTreeHash)
                do Assert.AreNotEqual(mf.IndexHash, mf.WorkingTreeHash)
            | _ ->
                do Assert.Fail("README.md should be in unstaged files")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }

    [<Test>]
    member _.GetWorkingDirectoryStatusCleanRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)

        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a clean Git repository with committed files
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create and commit files
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository") |> Async.AwaitTask
            do! File.WriteAllTextAsync(Path.Combine(testPath, "file1.txt"), "Content of file 1") |> Async.AwaitTask
            do! runGitCommandAsync(testPath, "add .")
            do! runGitCommandAsync(testPath, "commit -m \"Initial commit\"")

            use! repository = Repository.Factory.openStructured(testPath)

            let! status = repository.getWorkingDirectoryStatus()

            // Clean repository should have no changes at all (following git behavior)
            do Assert.AreEqual(0, status.StagedFiles.Count, "Clean repository should have no staged files");
            do Assert.AreEqual(0, status.UnstagedFiles.Count, "Clean repository should have no unstaged files");
            do Assert.AreEqual(0, status.UntrackedFiles.Count, "Clean repository should have no untracked files");
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                do Directory.Delete(testPath, true)
    }

    [<Test>]
    member _.GetWorkingDirectoryStatusWithFileProperties() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            do Directory.Delete(testPath, true)
        
        try
            do Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with a file
            do! runGitCommandAsync(testPath, "init")
            do! runGitCommandAsync(testPath, "config user.email \"test@example.com\"")
            do! runGitCommandAsync(testPath, "config user.name \"Test User\"")
            
            // Create some test files
            do! File.WriteAllTextAsync(Path.Combine(testPath, "test_file.txt"), "Test content") |> Async.AwaitTask

            use! repository = Repository.Factory.openStructured(testPath)

            let! status = repository.getWorkingDirectoryStatus()

            // Test file properties
            do Assert.IsTrue(status.UntrackedFiles.Count >= 1, "Should have at least one untracked file")
            let testFile = status.UntrackedFiles |> Seq.tryFind(fun f -> f.Path = "test_file.txt")
            match testFile with
            | Some tf ->
                do Assert.AreEqual("test_file.txt", tf.Path)
                do Assert.AreEqual(FileStatus.Untracked, tf.Status)
                do Assert.IsNull(tf.IndexHash, "Untracked file should not have index hash")
                do Assert.IsNotNull(tf.WorkingTreeHash, "Untracked file should have working tree hash")
            | _ ->
                do Assert.Fail("test_file.txt should be in untracked files")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true);
    }
