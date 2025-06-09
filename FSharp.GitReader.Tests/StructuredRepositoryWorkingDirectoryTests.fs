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
open VerifyNUnit

type public StructuredRepositoryWorkingDirectoryTests() =

    let private createValidGitIndex (indexPath: string) : Task =
        task {
            use fs = new FileStream(indexPath, FileMode.Create)
            use writer = new BinaryWriter(fs)
            // Write DIRC signature
            writer.Write([| 0x44uy; 0x49uy; 0x52uy; 0x43uy |]) // "DIRC"
            // Write version (2 in big-endian)
            writer.Write([| 0x00uy; 0x00uy; 0x00uy; 0x02uy |])
            // Write entry count (0 in big-endian)
            writer.Write([| 0x00uy; 0x00uy; 0x00uy; 0x00uy |])
        }

    [<Test>]
    member this.GetWorkingDirectoryStatusEmptyRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n")
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n")
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        let indexFile = Path.Combine(gitDir, "index")
        do! createValidGitIndex(indexFile)
        
        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Should have empty staged and unstaged files, but may have untracked files
        Assert.AreEqual(0, status.StagedFiles.Count)
        Assert.AreEqual(0, status.UnstagedFiles.Count)
        
        do! Verifier.Verify(status)
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusCleanRepository() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n")
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n")
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        let indexFile = Path.Combine(gitDir, "index")
        do! createValidGitIndex(indexFile)
        
        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Verify that clean repository has no changes
        Assert.AreEqual(0, status.StagedFiles.Count)
        Assert.AreEqual(0, status.UnstagedFiles.Count)
        
        do! Verifier.Verify(status)
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusWithFileProperties() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n")
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n")
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        let indexFile = Path.Combine(gitDir, "index")
        do! createValidGitIndex(indexFile)
        
        // Create some test files
        do! File.WriteAllTextAsync(Path.Combine(testPath, "test_file.txt"), "Test content")
        
        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Test that all files have valid status
        Assert.IsTrue(status.UntrackedFiles.Count >= 1)
        
        do! Verifier.Verify(status)
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member this.GetWorkingDirectoryStatusDeconstructorTest() = task {
        // Use a path outside the project directory to avoid parent directory search
        let testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n")
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n")
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        let indexFile = Path.Combine(gitDir, "index")
        do! createValidGitIndex(indexFile)
        
        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Test status deconstruction
        let (stagedFiles, unstagedFiles, untrackedFiles) = status
        
        Assert.AreSame(status.StagedFiles, stagedFiles)
        Assert.AreSame(status.UnstagedFiles, unstagedFiles)
        Assert.AreSame(status.UntrackedFiles, untrackedFiles)
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    } 