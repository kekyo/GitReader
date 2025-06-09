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
open System.IO
open System.Threading.Tasks
open VerifyNUnit

[<TestFixture>]
type PrimitiveRepositoryWorkingDirectoryTests() =

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
    member _.getWorkingDirectoryStatusEmptyRepository() : Task =
        task {
            // Use a path outside the project directory to avoid parent directory search
            let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
            
            // Create a minimal Git repository structure
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
            Directory.CreateDirectory(testPath) |> ignore
            let gitDir = Path.Combine(testPath, ".git")
            Directory.CreateDirectory(gitDir) |> ignore
            
            // Create minimal Git files
            do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
            Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
            
            // Create a valid empty Git index file to prevent reading from parent directories
            do! createValidGitIndex (Path.Combine(gitDir, "index"))

            use! repository = Repository.Factory.openPrimitive(testPath)
            
            let! status = repository.getWorkingDirectoryStatus()
            
            // Should have empty staged and unstaged files, but may have untracked files
            Assert.AreEqual(0, status.StagedFiles.Count)
            Assert.AreEqual(0, status.UnstagedFiles.Count)
            
            do! Verifier.Verify(status) |> Async.AwaitTask
            
            // Cleanup
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
        }

    [<Test>]
    member _.getWorkingDirectoryStatusCleanRepository() : Task =
        task {
            // Use a path outside the project directory to avoid parent directory search
            let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
            
            // Create a clean Git repository structure
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
            Directory.CreateDirectory(testPath) |> ignore
            let gitDir = Path.Combine(testPath, ".git")
            Directory.CreateDirectory(gitDir) |> ignore
            
            // Create minimal Git files
            do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
            Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
            
            // Create a valid empty Git index file to prevent reading from parent directories
            do! createValidGitIndex (Path.Combine(gitDir, "index"))

            use! repository = Repository.Factory.openPrimitive(testPath)
            
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
    member _.getWorkingDirectoryStatusDeconstructorTest() : Task =
        task {
            // Use a path outside the project directory to avoid parent directory search
            let testPath = Path.GetFullPath(Path.Combine("/tmp", $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"))
            
            // Create a minimal Git repository structure
            if Directory.Exists(testPath) then
                Directory.Delete(testPath, true)
            Directory.CreateDirectory(testPath) |> ignore
            let gitDir = Path.Combine(testPath, ".git")
            Directory.CreateDirectory(gitDir) |> ignore
            
            // Create minimal Git files
            do! File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n") |> Async.AwaitTask
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads")) |> ignore
            Directory.CreateDirectory(Path.Combine(gitDir, "objects")) |> ignore
            
            // Create a valid empty Git index file to prevent reading from parent directories
            do! createValidGitIndex (Path.Combine(gitDir, "index"))

            use! repository = Repository.Factory.openPrimitive(testPath)
            
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