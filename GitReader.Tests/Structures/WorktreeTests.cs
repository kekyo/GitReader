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

public sealed class WorktreeTests
{
    [Test]
    public async Task GetWorktreesMainOnly()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create initial commit
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository");
            await TestUtilities.RunGitCommandAsync(testPath, "add README.md");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var worktrees = await repository.GetWorktreesAsync();

            // Should have only main worktree
            Assert.AreEqual(1, worktrees.Count, "Should have exactly one worktree (main)");
            
            var mainWorktree = worktrees[0];
            Assert.AreEqual("(main)", mainWorktree.Name, "Main worktree should be named '(main)'");
            Assert.IsTrue(mainWorktree.IsMain, "Should be identified as main worktree");
            Assert.AreEqual(testPath, mainWorktree.Path, "Path should match repository root");
            Assert.AreEqual(WorktreeStatus.Normal, mainWorktree.Status, "Status should be Normal");
            Assert.IsNotNull(mainWorktree.Head, "Should have HEAD commit");
            Assert.IsNotNull(mainWorktree.Branch, "Should have current branch");
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
    public async Task GetWorktreesWithAdditionalWorktrees()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"));
        var worktree1Path = Path.Combine(testPath, "worktree1");
        var worktree2Path = Path.Combine(testPath, "worktree2");
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create initial commit
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository");
            await TestUtilities.RunGitCommandAsync(testPath, "add README.md");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Create branches
            await TestUtilities.RunGitCommandAsync(testPath, "branch feature1");
            await TestUtilities.RunGitCommandAsync(testPath, "branch feature2");
            
            // Create worktrees
            await TestUtilities.RunGitCommandAsync(testPath, $"worktree add {worktree1Path} feature1");
            await TestUtilities.RunGitCommandAsync(testPath, $"worktree add {worktree2Path} feature2");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var worktrees = await repository.GetWorktreesAsync();

            // Should have main + 2 additional worktrees
            Assert.AreEqual(3, worktrees.Count, "Should have exactly three worktrees");
            
            // Find main worktree
            var mainWorktree = worktrees.FirstOrDefault(w => w.IsMain);
            Assert.IsNotNull(mainWorktree, "Should have main worktree");
            Assert.AreEqual("(main)", mainWorktree!.Name);
            Assert.AreEqual(testPath, mainWorktree!.Path);
            Assert.AreEqual(WorktreeStatus.Normal, mainWorktree!.Status);
            
            // Find feature worktrees
            var feature1Worktree = worktrees.FirstOrDefault(w => w.Branch == "feature1");
            Assert.IsNotNull(feature1Worktree, "Should have feature1 worktree");
            Assert.AreEqual("worktree1", feature1Worktree!.Name);
            Assert.AreEqual(worktree1Path, feature1Worktree!.Path);
            Assert.AreEqual("feature1", feature1Worktree!.Branch);
            Assert.AreEqual(WorktreeStatus.Normal, feature1Worktree!.Status);
            Assert.IsFalse(feature1Worktree!.IsMain);
            
            var feature2Worktree = worktrees.FirstOrDefault(w => w.Branch == "feature2");
            Assert.IsNotNull(feature2Worktree, "Should have feature2 worktree");
            Assert.AreEqual("worktree2", feature2Worktree!.Name);
            Assert.AreEqual(worktree2Path, feature2Worktree!.Path);
            Assert.AreEqual("feature2", feature2Worktree!.Branch);
            Assert.AreEqual(WorktreeStatus.Normal, feature2Worktree!.Status);
            Assert.IsFalse(feature2Worktree!.IsMain);
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
    public async Task WorktreeDeconstructTest()
    {
        // Use a path outside the project directory to avoid parent directory search
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_WorktreeTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create initial commit
            await File.WriteAllTextAsync(Path.Combine(testPath, "README.md"), "# Test Repository");
            await TestUtilities.RunGitCommandAsync(testPath, "add README.md");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");

            using var repository = await Repository.Factory.OpenStructureAsync(testPath);

            var worktrees = await repository.GetWorktreesAsync();
            var mainWorktree = worktrees[0];
            
            // Test deconstruction
            var (name, path, head, branch, status) = mainWorktree;
            
            Assert.AreEqual(mainWorktree.Name, name, "Deconstructed name should match");
            Assert.AreEqual(mainWorktree.Path, path, "Deconstructed path should match");
            Assert.AreEqual(mainWorktree.Head, head, "Deconstructed head should match");
            Assert.AreEqual(mainWorktree.Branch, branch, "Deconstructed branch should match");
            Assert.AreEqual(mainWorktree.Status, status, "Deconstructed status should match");
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
