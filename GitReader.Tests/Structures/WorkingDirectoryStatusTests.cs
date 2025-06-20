////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace GitReader.Structures;

public sealed class WorkingDirectoryStatusTests
{
    [Test]
    public async Task GetWorkingDirectoryStatusEmptyRepository()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create an empty Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Empty repository should have no staged, unstaged, or untracked files
            Assert.AreEqual(0, status.StagedFiles.Count, "Empty repository should have no staged files");
            Assert.AreEqual(0, status.UnstagedFiles.Count, "Empty repository should have no unstaged files");
            Assert.AreEqual(0, status.UntrackedFiles.Count, "Empty repository should have no untracked files");
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
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository with initial commit
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create initial file and commit
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.");
            await TestUtilities.RunGitCommandAsync(testPath, "add README.md");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Create new untracked file
            await File.WriteAllTextAsync(Path.Combine(testPath, "new_file.txt"), "This is a new file for testing.");
            
            // Modify existing tracked file
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository\n\nInitial content.\n\nModified for testing.");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Should have untracked files (including new_file.txt)
            Assert.IsTrue(status.UntrackedFiles.Count > 0, "Should have untracked files");
            
            var newFile = status.UntrackedFiles.FirstOrDefault(f => f.Path == "new_file.txt");
            if (newFile.Path != null)
            {
                Assert.AreEqual(FileStatus.Untracked, newFile.Status);
                Assert.IsNull(newFile.IndexHash);
                Assert.IsNotNull(newFile.WorkingTreeHash);
            }
            else
            {
                Assert.Fail("new_file.txt should be in untracked files");
            }

            // README.md should be modified and appear in unstaged files
            var modifiedFile = status.UnstagedFiles.FirstOrDefault(f => f.Path == "README.md");
            if (modifiedFile.Path != null)
            {
                Assert.AreEqual(FileStatus.Modified, modifiedFile.Status);
                Assert.IsNotNull(modifiedFile.IndexHash);
                Assert.IsNotNull(modifiedFile.WorkingTreeHash);
                Assert.AreNotEqual(modifiedFile.IndexHash, modifiedFile.WorkingTreeHash);
            }
            else
            {
                Assert.Fail("README.md should be in unstaged files");
            }
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
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }

        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a clean Git repository with committed files
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create and commit files
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository");
            await File.WriteAllTextAsync(Path.Combine(testPath, "file1.txt"), "Content of file 1");
            await TestUtilities.RunGitCommandAsync(testPath, "add .");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Clean repository should have no changes at all (following git behavior)
            Assert.AreEqual(0, status.StagedFiles.Count, "Clean repository should have no staged files");
            Assert.AreEqual(0, status.UnstagedFiles.Count, "Clean repository should have no unstaged files");
            Assert.AreEqual(0, status.UntrackedFiles.Count, "Clean repository should have no untracked files");
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
    public async Task GetWorkingDirectoryStatusWithFileProperties()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository with a file
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create some test files
            await File.WriteAllTextAsync(Path.Combine(testPath, "test_file.txt"), "Test content");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Test file properties
            Assert.IsTrue(status.UntrackedFiles.Count >= 1, "Should have at least one untracked file");
            var testFile = status.UntrackedFiles.FirstOrDefault(f => f.Path == "test_file.txt");
            if (testFile.Path != null)
            {
                Assert.AreEqual("test_file.txt", testFile.Path);
                Assert.AreEqual(FileStatus.Untracked, testFile.Status);
                Assert.IsNull(testFile.IndexHash, "Untracked file should not have index hash");
                Assert.IsNotNull(testFile.WorkingTreeHash, "Untracked file should have working tree hash");
            }
            else
            {
                Assert.Fail("test_file.txt should be in untracked files");
            }
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
    public async Task GetWorkingDirectoryStatusDeconstructorTest()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorkingDirTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create an empty Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);
            
            var status = await repository.GetWorkingDirectoryStatusAsync();
            
            // Test status deconstruction
            var (stagedFiles, unstagedFiles, untrackedFiles) = status;
            
            Assert.AreSame(status.StagedFiles, stagedFiles, "Deconstructed StagedFiles should be same reference");
            Assert.AreSame(status.UnstagedFiles, unstagedFiles, "Deconstructed UnstagedFiles should be same reference");
            Assert.AreSame(status.UntrackedFiles, untrackedFiles, "Deconstructed UntrackedFiles should be same reference");
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
} 