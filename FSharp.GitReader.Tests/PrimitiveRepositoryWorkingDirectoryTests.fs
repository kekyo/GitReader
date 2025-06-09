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
open GitReader.Structures
open NUnit.Framework
open System.IO
open System.Threading.Tasks
open System.Diagnostics

type public PrimitiveRepositoryWorkingDirectoryTests() =

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
            
            use! repository = Repository.Factory.openPrimitive(testPath)
            
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
            
            use! repository = Repository.Factory.openPrimitive(testPath)
            
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
    member this.GetWorkingDirectoryStatusWithModificationsTest() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        try
            Directory.CreateDirectory(testPath) |> ignore
            
            // Create a Git repository with initial commit
            do! runGitCommandAsync testPath "init"
            do! runGitCommandAsync testPath "config user.email \"test@example.com\""
            do! runGitCommandAsync testPath "config user.name \"Test User\""
            
            // Create initial file and commit
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.")
            do! runGitCommandAsync testPath "add README.md"
            do! runGitCommandAsync testPath "commit -m \"Initial commit\""
            
            // Create new untracked file
            do! File.WriteAllTextAsync(Path.Combine(testPath, "new_file.txt"), "This is a new file for testing.")
            
            // Modify existing tracked file
            do! File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.\n\nModified for testing.")
            
            use! repository = Repository.Factory.openPrimitive(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Should have untracked files (including new_file.txt)
            Assert.IsTrue(status.UntrackedFiles.Count > 0, "Should have untracked files")
            
            let newFile = status.UntrackedFiles |> Seq.tryFind (fun f -> f.Path = "new_file.txt")
            Assert.IsTrue(newFile.IsSome, "new_file.txt should be in untracked files")
            
            match newFile with
            | Some file ->
                Assert.AreEqual(FileStatus.Untracked, enum<FileStatus> file.Status)
                Assert.IsNull(file.IndexHash)
                Assert.IsNotNull(file.WorkingTreeHash)
            | None -> ()

            // README.md should be modified and appear in unstaged files
            let modifiedFile = status.UnstagedFiles |> Seq.tryFind (fun f -> f.Path = "README.md")
            Assert.IsTrue(modifiedFile.IsSome, "README.md should be in unstaged files")
            
            match modifiedFile with
            | Some file ->
                Assert.AreEqual(FileStatus.Modified, enum<FileStatus> file.Status)
                Assert.IsNotNull(file.IndexHash)
                Assert.IsNotNull(file.WorkingTreeHash)
                Assert.AreNotEqual(file.IndexHash, file.WorkingTreeHash)
            | None -> ()
        finally
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
    } 