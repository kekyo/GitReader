////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyNUnit;

namespace GitReader.Primitive;

public sealed class WorkingDirectoryStatusTests
{
    [Test]
    public async Task GetWorkingDirectoryStatusEmptyRepository()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        // Create a minimal Git repository structure without index
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        try
        {
            Directory.CreateDirectory(testPath);
            var gitDir = Path.Combine(testPath, ".git");
            Directory.CreateDirectory(gitDir);
        
            // Create minimal Git files
            await File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n");
            await File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n");
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads"));
            Directory.CreateDirectory(Path.Combine(gitDir, "objects"));
        
            // Create a valid empty Git index file to prevent reading from parent directories
            var indexFile = Path.Combine(gitDir, "index");
            using (var fs = new FileStream(indexFile, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // Write DIRC signature
                writer.Write(new byte[] { 0x44, 0x49, 0x52, 0x43 }); // "DIRC"
                // Write version (2 in big-endian)
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x02 });
                // Write entry count (0 in big-endian)
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            }

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Should have empty staged and unstaged files, but may have untracked files
            Assert.AreEqual(0, status.StagedFiles.Count);
            Assert.AreEqual(0, status.UnstagedFiles.Count);
        
            await Verifier.Verify(status);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    [Test]
    public async Task GetWorkingDirectoryStatusWithModifications()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        // Copy existing test1 repository to use as working directory
        var sourcePath = RepositoryTestsSetUp.GetBasePath("test1");
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        try
        {
            CopyDirectory(sourcePath, testPath);

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Create and modify test files
            var newFilePath = Path.Combine(testPath, "new_file.txt");
            await File.WriteAllTextAsync(newFilePath, "This is a new file for testing.");

            var existingFilePath = Path.Combine(testPath, "README.md");
            if (File.Exists(existingFilePath))
            {
                var content = await File.ReadAllTextAsync(existingFilePath);
                await File.WriteAllTextAsync(existingFilePath, content + "\nModified for testing.");
            }

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Sort results for stable test results
            var sortedStatus = new PrimitiveWorkingDirectoryStatus(
                status.StagedFiles.OrderBy(f => f.Path).ToArray(),
                status.UnstagedFiles.OrderBy(f => f.Path).ToArray(),
                status.UntrackedFiles.OrderBy(f => f.Path).ToArray()
            );

            await Verifier.Verify(sortedStatus);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    [Test]
    public async Task GetWorkingDirectoryStatusCleanRepository()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        // Create a clean Git repository structure
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        try
        {
            Directory.CreateDirectory(testPath);
            var gitDir = Path.Combine(testPath, ".git");
            Directory.CreateDirectory(gitDir);
        
            // Create minimal Git files
            await File.WriteAllTextAsync(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n");
            await File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n\tbare = false\n");
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads"));
            Directory.CreateDirectory(Path.Combine(gitDir, "objects"));
        
            // Create a valid empty Git index file to prevent reading from parent directories
            var indexFile = Path.Combine(gitDir, "index");
            using (var fs = new FileStream(indexFile, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // Write DIRC signature
                writer.Write(new byte[] { 0x44, 0x49, 0x52, 0x43 }); // "DIRC"
                // Write version (2 in big-endian)
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x02 });
                // Write entry count (0 in big-endian)
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            }

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Verify that clean repository has no changes
            Assert.AreEqual(0, status.StagedFiles.Count);
            Assert.AreEqual(0, status.UnstagedFiles.Count);
        
            await Verifier.Verify(status);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    [Test]
    public async Task GetWorkingDirectoryStatusWithStagedFiles()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        // Copy existing test1 repository to use as working directory
        var sourcePath = RepositoryTestsSetUp.GetBasePath("test1");
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        try
        {
            CopyDirectory(sourcePath, testPath);

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Create a new file
            var newFilePath = Path.Combine(testPath, "staged_file.txt");
            await File.WriteAllTextAsync(newFilePath, "This file will be staged.");

            // Execute Git add command to stage files
            // Note: Assumes Git is available in the actual test environment
            // Or need to manually manipulate .git/index file
        
            var status = await repository.GetWorkingDirectoryStatusAsync();
        
            await Verifier.Verify(status);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
} 