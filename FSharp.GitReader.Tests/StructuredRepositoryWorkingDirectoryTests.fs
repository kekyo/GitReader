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
    member __.GetWorkingDirectoryStatusEmptyRepository() = task {
        let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n") |> Async.AwaitTask
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        do! createValidGitIndex (Path.Combine(gitDir, "index"))

        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Should have empty staged and unstaged files
        Assert.AreEqual(0, status.StagedFiles.Count)
        Assert.AreEqual(0, status.UnstagedFiles.Count)
        
        do! Verifier.Verify(status) |> Async.AwaitTask
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member __.GetWorkingDirectoryStatusCleanRepository() = task {
        let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a clean Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n") |> Async.AwaitTask
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        do! createValidGitIndex (Path.Combine(gitDir, "index"))

        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Verify that clean repository has no changes
        Assert.AreEqual(0, status.StagedFiles.Count)
        Assert.AreEqual(0, status.UnstagedFiles.Count)
        
        do! Verifier.Verify(status) |> Async.AwaitTask
        
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member __.WorkingDirectoryStatusFileTypes() = task {
        let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a Git repository structure with some files
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n") |> Async.AwaitTask
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        do! createValidGitIndex (Path.Combine(gitDir, "index"))
        
        // Create some test files
        do! File.WriteAllTextAsync(Path.Combine(testPath, "test_file.txt"), "Test content") |> Async.AwaitTask

        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Test that all files have valid status
        for file in status.StagedFiles do
            Assert.IsFalse(System.String.IsNullOrEmpty(file.Path))
            Assert.AreNotEqual(FileStatus.Unmodified, file.Status)

        for file in status.UnstagedFiles do
            Assert.IsFalse(System.String.IsNullOrEmpty(file.Path))
            Assert.AreNotEqual(FileStatus.Unmodified, file.Status)
            
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    }

    [<Test>]
    member __.WorkingDirectoryStatusDeconstruction() = task {
        let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
        
        // Create a minimal Git repository structure
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
        Directory.CreateDirectory(testPath) |> ignore
        let gitDir = Path.Combine(testPath, ".git")
        Directory.CreateDirectory(gitDir) |> ignore
        
        // Create minimal Git files
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
        do! File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n") |> Async.AwaitTask
        Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
        Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
        
        // Create a valid empty Git index file to prevent reading from parent directories
        do! createValidGitIndex (Path.Combine(gitDir, "index"))

        use! repository = Repository.Factory.openStructure(testPath)
        
        let! status = repository.getWorkingDirectoryStatus()
        
        // Test status deconstruction
        let (stagedFiles, unstagedFiles, untrackedFiles) = status
        Assert.AreEqual(stagedFiles, status.StagedFiles)
        Assert.AreEqual(unstagedFiles, status.UnstagedFiles)
        Assert.AreEqual(untrackedFiles, status.UntrackedFiles)

        // Test file deconstruction if any files exist
        if status.StagedFiles.Count > 0 then
            let file = status.StagedFiles.[0]
            let (path, fileStatus, indexHash, workingTreeHash) = file
            Assert.AreEqual(path, file.Path)
            Assert.AreEqual(fileStatus, file.Status)
            Assert.AreEqual(indexHash, file.IndexHash |> wrapOptionV)
            Assert.AreEqual(workingTreeHash, file.WorkingTreeHash |> wrapOptionV)
            
        // Cleanup
        if Directory.Exists(testPath) then
            Directory.Delete(testPath, true)
    } 