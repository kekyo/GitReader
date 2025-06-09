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
open System.Threading.Tasks
open System.Diagnostics

type public StructuredRepositoryWorkingDirectoryTests() =

    let private runGitCommandAsync (workingDirectory: string) (arguments: string) : Task =
        task {
            let startInfo = ProcessStartInfo(
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            )

            use process = new Process(StartInfo = startInfo)
            process.Start() |> ignore
            do! process.WaitForExitAsync()

            if process.ExitCode <> 0 then
                let! error = process.StandardError.ReadToEndAsync()
                failwith $"Git command failed: git {arguments}\nError: {error}"
        }

    [<Test>]
    member this.GetWorkingDirectoryStatusEmptyRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        try
            Directory.CreateDirectory(testPath) |> ignore
            
            // Create an empty Git repository
            do! runGitCommandAsync testPath "init"
            do! runGitCommandAsync testPath "config user.email \"test@example.com\""
            do! runGitCommandAsync testPath "config user.name \"Test User\""
            
            use! repository = Repository.Factory.openStructure(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Empty repository should have no staged, unstaged, or untracked files
            Assert.AreEqual(0, status.StagedFiles.Count, "Empty repository should have no staged files")
            Assert.AreEqual(0, status.UnstagedFiles.Count, "Empty repository should have no unstaged files")
            Assert.AreEqual(0, status.UntrackedFiles.Count, "Empty repository should have no untracked files")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusCleanRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        try
            Directory.CreateDirectory(testPath) |> ignore
            
            // Create a clean Git repository with committed files
            do! runGitCommandAsync testPath "init"
            do! runGitCommandAsync testPath "config user.email \"test@example.com\""
            do! runGitCommandAsync testPath "config user.name \"Test User\""
            
            // Create and commit files
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository")
            do! File.WriteAllTextAsync(Path.Combine(testPath, "file1.txt"), "Content of file 1")
            do! runGitCommandAsync testPath "add ."
            do! runGitCommandAsync testPath "commit -m \"Initial commit\""
            
            use! repository = Repository.Factory.openStructure(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Clean repository should have no changes at all (following git behavior)
            Assert.AreEqual(0, status.StagedFiles.Count, "Clean repository should have no staged files")
            Assert.AreEqual(0, status.UnstagedFiles.Count, "Clean repository should have no unstaged files")
            Assert.AreEqual(0, status.UntrackedFiles.Count, "Clean repository should have no untracked files")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusWithFileProperties() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        try
            Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with a file
            do! runGitCommandAsync testPath "init"
            do! runGitCommandAsync testPath "config user.email \"test@example.com\""
            do! runGitCommandAsync testPath "config user.name \"Test User\""
            
            // Create some test files
            do! File.WriteAllTextAsync(Path.Combine(testPath, "test_file.txt"), "Test content")
            
            use! repository = Repository.Factory.openStructure(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Test that untracked files are detected correctly
            Assert.IsTrue(status.UntrackedFiles.Count >= 1, "Should have at least one untracked file")
            
            let testFile = status.UntrackedFiles |> Seq.tryFind (fun f -> f.Path = "test_file.txt")
            Assert.IsTrue(testFile.IsSome, "test_file.txt should be in untracked files")
            
            match testFile with
            | Some file ->
                Assert.AreEqual("test_file.txt", file.Path)
                Assert.AreEqual(FileStatus.Untracked, file.Status)
                Assert.IsNull(file.IndexHash, "Untracked file should not have index hash")
                Assert.IsNotNull(file.WorkingTreeHash, "Untracked file should have working tree hash")
            | None -> ()
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusDeconstructorTest() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        try
            Directory.CreateDirectory(testPath) |> ignore
            
            // Create an empty Git repository
            do! runGitCommandAsync testPath "init"
            do! runGitCommandAsync testPath "config user.email \"test@example.com\""
            do! runGitCommandAsync testPath "config user.name \"Test User\""
            
            use! repository = Repository.Factory.openStructure(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Test status deconstruction
            let (stagedFiles, unstagedFiles, untrackedFiles) = status
            
            Assert.AreSame(status.StagedFiles, stagedFiles, "Deconstructed StagedFiles should be same reference")
            Assert.AreSame(status.UnstagedFiles, unstagedFiles, "Deconstructed UnstagedFiles should be same reference")
            Assert.AreSame(status.UntrackedFiles, untrackedFiles, "Deconstructed UntrackedFiles should be same reference")
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
    } 