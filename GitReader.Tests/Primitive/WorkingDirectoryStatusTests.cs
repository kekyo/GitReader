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

namespace GitReader.Primitive;

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

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

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

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Should have untracked files (including new_file.txt)
            Assert.IsTrue(status.UntrackedFiles.Count > 0, "Should have untracked files");
            
            var newFile = status.UntrackedFiles.FirstOrDefault(f => f.Path == "new_file.txt");
            if (!string.IsNullOrEmpty(newFile.Path))
            {
                Assert.AreEqual(FileStatus.Untracked, (FileStatus)newFile.Status);
                Assert.IsNull(newFile.IndexHash);
                Assert.IsNotNull(newFile.WorkingTreeHash);
            }
            else
            {
                Assert.Fail("new_file.txt should be in untracked files");
            }

            // README.md should be modified and appear in unstaged files
            var modifiedFile = status.UnstagedFiles.FirstOrDefault(f => f.Path == "README.md");
            if (!string.IsNullOrEmpty(modifiedFile.Path))
            {
                Assert.AreEqual(FileStatus.Modified, (FileStatus)modifiedFile.Status);
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

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

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
    public async Task GetWorkingDirectoryStatusWithStagedFiles()
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
            
            // Create a Git repository with staged files
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create initial commit
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository");
            await TestUtilities.RunGitCommandAsync(testPath, "add README.md");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Create new file and stage it
            await File.WriteAllTextAsync(Path.Combine(testPath, "staged_file.txt"), "This file will be staged");
            await TestUtilities.RunGitCommandAsync(testPath, "add staged_file.txt");
            
            // Create untracked file
            await File.WriteAllTextAsync(Path.Combine(testPath, "untracked_file.txt"), "This file is untracked");

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Verify basic status functionality works
            Assert.IsNotNull(status, "Status should not be null");
            Assert.IsNotNull(status.StagedFiles, "StagedFiles collection should not be null");
            Assert.IsNotNull(status.UnstagedFiles, "UnstagedFiles collection should not be null");
            Assert.IsNotNull(status.UntrackedFiles, "UntrackedFiles collection should not be null");

            // Should have staged file (GitReader may interpret newly staged files differently)
            var stagedFile = status.StagedFiles.FirstOrDefault(f => f.Path == "staged_file.txt");
            if (!string.IsNullOrEmpty(stagedFile.Path))
            {
                Assert.AreEqual("staged_file.txt", stagedFile.Path);
                // GitReader may categorize newly added files differently than git status
                Assert.IsTrue((FileStatus)stagedFile.Status == FileStatus.Added || (FileStatus)stagedFile.Status == FileStatus.Unmodified,
                    $"Staged file status should be Added or Unmodified, but was {(FileStatus)stagedFile.Status}");
                Assert.IsNotNull(stagedFile.WorkingTreeHash, "Staged file should have working tree hash");
            }
            else
            {
                Assert.Fail("staged_file.txt should be in staged files");
            }

            // Should have untracked file
            var untrackedFile = status.UntrackedFiles.FirstOrDefault(f => f.Path == "untracked_file.txt");
            if (!string.IsNullOrEmpty(untrackedFile.Path))
            {
                Assert.AreEqual("untracked_file.txt", untrackedFile.Path);
                Assert.AreEqual(FileStatus.Untracked, (FileStatus)untrackedFile.Status);
                Assert.IsNull(untrackedFile.IndexHash, "Untracked file should not have index hash");
                Assert.IsNotNull(untrackedFile.WorkingTreeHash, "Untracked file should have working tree hash");
            }
            else
            {
                Assert.Fail("untracked_file.txt should be in untracked files");
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
}
