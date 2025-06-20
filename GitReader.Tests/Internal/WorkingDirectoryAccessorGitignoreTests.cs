////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

/// <summary>
/// Tests for WorkingDirectoryAccessor's .gitignore functionality.
/// </summary>
[TestFixture]
public sealed class WorkingDirectoryAccessorGitignoreTests
{
    private string testPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_GitignoreTest_{Guid.NewGuid():N}"));
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        Directory.CreateDirectory(testPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
    }

    /// <summary>
    /// Tests that root .gitignore is applied correctly.
    /// </summary>
    [Test]
    public async Task TestRootGitignoreFiltering()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create .gitignore in root
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\ntemp/\n");
        
        // Create test files
        var logFile = Path.Combine(testPath, "debug.log");
        var csFile = Path.Combine(testPath, "Program.cs");
        var tempDir = Path.Combine(testPath, "temp");
        var tempFile = Path.Combine(tempDir, "temp.txt");
        
        await File.WriteAllTextAsync(logFile, "log content");
        await File.WriteAllTextAsync(csFile, "cs content");
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(tempFile, "temp content");

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Get working directory status with include-all filter (default behavior)
        var status = await repository.GetWorkingDirectoryStatusAsync();

        // Verify that .log files and temp/ directory are filtered out
        var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // Debug output
        Console.WriteLine($"Found {untrackedFilePaths.Length} untracked files:");
        foreach (var file in untrackedFilePaths)
        {
            Console.WriteLine($"  - {file}");
        }
        
        Assert.Contains("Program.cs", untrackedFilePaths);
        Assert.IsFalse(untrackedFilePaths.Contains("debug.log"));
        Assert.IsFalse(untrackedFilePaths.Contains("temp/temp.txt"));
        Assert.IsTrue(untrackedFilePaths.Contains(".gitignore")); // .gitignore itself should be included (Git's official behavior)
    }

    /// <summary>
    /// Tests that nested .gitignore files override parent ones correctly.
    /// </summary>
    [Test]
    public async Task TestNestedGitignoreOverride()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create root .gitignore
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\n");
        
        // Create src directory with its own .gitignore
        var srcDir = Path.Combine(testPath, "src");
        Directory.CreateDirectory(srcDir);
        var srcGitignore = Path.Combine(srcDir, ".gitignore");
        await File.WriteAllTextAsync(srcGitignore, "!important.log\n*.tmp\n");
        
        // Create test files
        var rootLogFile = Path.Combine(testPath, "debug.log");
        var srcLogFile = Path.Combine(srcDir, "important.log");
        var srcTmpFile = Path.Combine(srcDir, "temp.tmp");
        var srcCsFile = Path.Combine(srcDir, "Program.cs");
        
        await File.WriteAllTextAsync(rootLogFile, "root log");
        await File.WriteAllTextAsync(srcLogFile, "important log");
        await File.WriteAllTextAsync(srcTmpFile, "temp content");
        await File.WriteAllTextAsync(srcCsFile, "cs content");

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Get working directory status
        var status = await repository.GetWorkingDirectoryStatusAsync();

        var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // Root .log files should be ignored
        Assert.IsFalse(untrackedFilePaths.Contains("debug.log"));
        
        // important.log should be included due to negation in src/.gitignore
        Assert.Contains("src/important.log", untrackedFilePaths);
        
        // .tmp files should be ignored by src/.gitignore
        Assert.IsFalse(untrackedFilePaths.Contains("src/temp.tmp"));
        
        // .cs files should be included
        Assert.Contains("src/Program.cs", untrackedFilePaths);
    }

    /// <summary>
    /// Tests that pathFilter has priority over .gitignore.
    /// </summary>
    [Test]
    public async Task TestPathFilterPriorityOverGitignore()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create .gitignore that ignores *.cs files
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.cs\n");
        
        // Create test files
        var csFile = Path.Combine(testPath, "Program.cs");
        var jsFile = Path.Combine(testPath, "script.js");
        
        await File.WriteAllTextAsync(csFile, "cs content");
        await File.WriteAllTextAsync(jsFile, "js content");

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Create pathFilter that specifically neutral *.cs files
        GlobFilter pathFilter = (_, path) =>
            path.EndsWith(".cs") ?
                GlobFilterStates.NotExclude : GlobFilterStates.Exclude;

        // Get working directory status with pathFilter
        var status = await repository.GetWorkingDirectoryStatusAsync();
        var untrackedFiles = await repository.GetUntrackedFilesAsync(status, pathFilter);

        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // pathFilter should override .gitignore - .cs file should be included
        Assert.Contains("Program.cs", untrackedFilePaths);
        
        // .js file should be excluded by pathFilter (not matching *.cs pattern)
        Assert.IsFalse(untrackedFilePaths.Contains("script.js"));
    }

    /// <summary>
    /// Tests complex scenario with multiple directory levels and .gitignore files.
    /// </summary>
    [Test]
    public async Task TestComplexMultiLevelGitignore()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create directory structure: root/src/main/java
        var srcDir = Path.Combine(testPath, "src");
        var mainDir = Path.Combine(srcDir, "main");
        var javaDir = Path.Combine(mainDir, "java");
        Directory.CreateDirectory(javaDir);
        
        // Create .gitignore files at different levels
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\ntarget/\n");
        
        var srcGitignore = Path.Combine(srcDir, ".gitignore");
        await File.WriteAllTextAsync(srcGitignore, "*.tmp\n!important.tmp\n");
        
        var javaGitignore = Path.Combine(javaDir, ".gitignore");
        await File.WriteAllTextAsync(javaGitignore, "*.class\n!Main.class\n");
        
        // Create test files
        var files = new (string, string)[]
        {
            (Path.Combine(testPath, "debug.log"), "log"),
            (Path.Combine(testPath, "README.md"), "readme"),
            (Path.Combine(srcDir, "temp.tmp"), "temp"),
            (Path.Combine(srcDir, "important.tmp"), "important temp"),
            (Path.Combine(mainDir, "config.xml"), "config"),
            (Path.Combine(javaDir, "Test.class"), "test class"),
            (Path.Combine(javaDir, "Main.class"), "main class"),
            (Path.Combine(javaDir, "App.java"), "java source")
        };
        
        foreach (var (filePath, content) in files)
        {
            await File.WriteAllTextAsync(filePath, content);
        }

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Get working directory status
        var status = await repository.GetWorkingDirectoryStatusAsync();

        var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();

        // Files that should be included
        Assert.Contains("README.md", untrackedFilePaths);
        Assert.Contains("src/important.tmp", untrackedFilePaths); // Negated in src/.gitignore
        Assert.Contains("src/main/config.xml", untrackedFilePaths);
        Assert.Contains("src/main/java/Main.class", untrackedFilePaths); // Negated in java/.gitignore
        Assert.Contains("src/main/java/App.java", untrackedFilePaths);

        // Files that should be excluded
        Assert.IsFalse(untrackedFilePaths.Contains("debug.log")); // Ignored by root
        Assert.IsFalse(untrackedFilePaths.Contains("src/temp.tmp")); // Ignored by src
        Assert.IsFalse(untrackedFilePaths.Contains("src/main/java/Test.class")); // Ignored by java
    }

    /// <summary>
    /// Tests that .gitignore is combined with custom pathFilter correctly.
    /// </summary>
    [Test]
    public async Task TestGitignoreWithCustomPathFilter()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create .gitignore
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\n");
        
        // Create test files
        var files = new (string, string)[]
        {
            (Path.Combine(testPath, "Program.cs"), "cs content"),
            (Path.Combine(testPath, "script.js"), "js content"),
            (Path.Combine(testPath, "debug.log"), "log content"),
            (Path.Combine(testPath, "README.md"), "readme")
        };
        
        foreach (var (filePath, content) in files)
        {
            await File.WriteAllTextAsync(filePath, content);
        }

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Create pathFilter that only includes source files (.cs, .js)
        GlobFilter pathFilter = (_, path) =>
            (path.EndsWith(".cs") || path.EndsWith(".js")) ?
                GlobFilterStates.NotExclude : GlobFilterStates.Exclude;

        // Get working directory status with pathFilter
        var status = await repository.GetWorkingDirectoryStatusAsync();

        var untrackedFiles = await repository.GetUntrackedFilesAsync(status, pathFilter);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // Should include source files that pass pathFilter
        Assert.Contains("Program.cs", untrackedFilePaths);
        Assert.Contains("script.js", untrackedFilePaths);
        
        // Should exclude .log (by .gitignore) even if pathFilter would allow it
        Assert.IsFalse(untrackedFilePaths.Contains("debug.log"));
        
        // Should exclude .md (by pathFilter)
        Assert.IsFalse(untrackedFilePaths.Contains("README.md"));
    }

    /// <summary>
    /// Tests that files staged before .gitignore creation remain tracked.
    /// </summary>
    [Test]
    public async Task TestGitignoreWithFilesStageBeforeGitignore()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create and commit initial files
        var committedFile = Path.Combine(testPath, "committed.txt");
        await File.WriteAllTextAsync(committedFile, "initial content");
        await TestUtilities.RunGitCommandAsync(testPath, "add committed.txt");
        await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
        
        // Create and stage file BEFORE .gitignore exists
        var stagedLogFile = Path.Combine(testPath, "staged.log");
        await File.WriteAllTextAsync(stagedLogFile, "staged log content");
        await TestUtilities.RunGitCommandAsync(testPath, "add staged.log");
        
        // Create .gitignore AFTER staging the file
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\n");
        
        // Modify committed file
        await File.WriteAllTextAsync(committedFile, "modified content");
        
        // Create untracked file that matches .gitignore pattern
        var untrackedLogFile = Path.Combine(testPath, "untracked.log");
        await File.WriteAllTextAsync(untrackedLogFile, "untracked log content");
        
        // Create untracked file that doesn't match .gitignore pattern
        var untrackedCsFile = Path.Combine(testPath, "untracked.cs");
        await File.WriteAllTextAsync(untrackedCsFile, "cs content");

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Get working directory status
        var status = await repository.GetWorkingDirectoryStatusAsync();

        var stagedFiles = status.StagedFiles.Select(f => f.Path).ToArray();
        var unstagedFiles = status.UnstagedFiles.Select(f => f.Path).ToArray();
        var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // Files staged before .gitignore should remain staged regardless of .gitignore
        Assert.Contains("staged.log", stagedFiles);
        
        // Modified files should be included regardless of .gitignore  
        Assert.Contains("committed.txt", unstagedFiles);
        
        // Untracked files should respect .gitignore
        Assert.Contains("untracked.cs", untrackedFilePaths);
        Assert.IsFalse(untrackedFilePaths.Contains("untracked.log"));
    }

    /// <summary>
    /// Tests that files can be force-added despite .gitignore rules.
    /// </summary>
    [Test]
    public async Task TestGitignoreWithForceAddedFiles()
    {
        // Setup git repository
        await TestUtilities.RunGitCommandAsync(testPath, "init");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
        await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
        
        // Create and commit initial files
        var committedFile = Path.Combine(testPath, "committed.txt");
        await File.WriteAllTextAsync(committedFile, "initial content");
        await TestUtilities.RunGitCommandAsync(testPath, "add committed.txt");
        await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
        
        // Create .gitignore
        var rootGitignore = Path.Combine(testPath, ".gitignore");
        await File.WriteAllTextAsync(rootGitignore, "*.log\n");
        
        // Modify committed file
        await File.WriteAllTextAsync(committedFile, "modified content");
        
        // Create staged file that matches .gitignore pattern and force-add it
        var stagedLogFile = Path.Combine(testPath, "staged.log");
        await File.WriteAllTextAsync(stagedLogFile, "staged log content");
        await TestUtilities.RunGitCommandAsync(testPath, "add -f staged.log");
        
        // Create untracked file that matches .gitignore pattern
        var untrackedLogFile = Path.Combine(testPath, "untracked.log");
        await File.WriteAllTextAsync(untrackedLogFile, "untracked log content");
        
        // Create untracked file that doesn't match .gitignore pattern
        var untrackedCsFile = Path.Combine(testPath, "untracked.cs");
        await File.WriteAllTextAsync(untrackedCsFile, "cs content");

        using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

        // Get working directory status
        var status = await repository.GetWorkingDirectoryStatusAsync();

        var stagedFiles = status.StagedFiles.Select(f => f.Path).ToArray();
        var unstagedFiles = status.UnstagedFiles.Select(f => f.Path).ToArray();
        var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
        var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
        
        // Force-added files should be staged regardless of .gitignore
        Assert.Contains("staged.log", stagedFiles);
        
        // Modified files should be included regardless of .gitignore  
        Assert.Contains("committed.txt", unstagedFiles);
        
        // Untracked files should respect .gitignore
        Assert.Contains("untracked.cs", untrackedFilePaths);
        Assert.IsFalse(untrackedFilePaths.Contains("untracked.log"));
    }

    [Test]
    public async Task TestGitDirectoryExclusion()
    {
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_GitDirTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Initialize a Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create test files
            await File.WriteAllTextAsync(Path.Combine(testPath, "normal_file.txt"), "normal content");
            await File.WriteAllTextAsync(Path.Combine(testPath, ".git_like_file"), "not a real git file");
            
            // Create .git-like directory (should be included)
            var gitLikeDir = Path.Combine(testPath, ".git_like_dir");
            Directory.CreateDirectory(gitLikeDir);
            await File.WriteAllTextAsync(Path.Combine(gitLikeDir, "file.txt"), "content");
            
            // Create additional files inside .git directory (these should be excluded)
            var gitPath = Path.Combine(testPath, ".git");
            await File.WriteAllTextAsync(Path.Combine(gitPath, "test_file"), "should be excluded");
            Directory.CreateDirectory(Path.Combine(gitPath, "test_objects"));
            await File.WriteAllTextAsync(Path.Combine(gitPath, "test_objects", "test"), "object content");
            
            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);
            var status = await repository.GetWorkingDirectoryStatusAsync();
            
            var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
            var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
            
            // Debug output
            Console.WriteLine($"Found {untrackedFilePaths.Length} untracked files:");
            foreach (var file in untrackedFilePaths)
            {
                Console.WriteLine($"  - {file}");
            }
            
            // Verify .git directory and its contents are excluded
            Assert.IsFalse(untrackedFilePaths.Any(f => f.StartsWith(".git/")), ".git directory contents should be excluded");
            Assert.IsFalse(untrackedFilePaths.Contains(".git"), ".git directory itself should be excluded");
            
            // Verify .git-like files are included (not confused with .git)
            Assert.IsTrue(untrackedFilePaths.Contains(".git_like_file"), ".git_like_file should be included");
            Assert.IsTrue(untrackedFilePaths.Contains(".git_like_dir/file.txt"), ".git_like_dir contents should be included");
            Assert.IsTrue(untrackedFilePaths.Contains("normal_file.txt"), "normal files should be included");
        }
        finally
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    [Test]
    public async Task TestGitDirectoryExclusionInSubdirectories()
    {
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_GitDirSubTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Initialize a Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create normal files in subdirectories
            var subDir1 = Path.Combine(testPath, "project", "src");
            var subDir2 = Path.Combine(testPath, "vendor", "lib");
            Directory.CreateDirectory(subDir1);
            Directory.CreateDirectory(subDir2);
            
            await File.WriteAllTextAsync(Path.Combine(subDir1, "main.cpp"), "int main() { return 0; }");
            await File.WriteAllTextAsync(Path.Combine(subDir2, "helper.js"), "function help() {}");
            
            // Create .git directories in subdirectories (these should be excluded)
            var gitInSub1 = Path.Combine(subDir1, ".git");
            var gitInSub2 = Path.Combine(subDir2, ".git");
            Directory.CreateDirectory(gitInSub1);
            Directory.CreateDirectory(gitInSub2);
            
            // Create files inside subdirectory .git directories (should all be excluded)
            await File.WriteAllTextAsync(Path.Combine(gitInSub1, "config"), "[core]\nrepositoryformatversion = 0");
            await File.WriteAllTextAsync(Path.Combine(gitInSub1, "HEAD"), "ref: refs/heads/main");
            Directory.CreateDirectory(Path.Combine(gitInSub1, "objects"));
            await File.WriteAllTextAsync(Path.Combine(gitInSub1, "objects", "test1"), "object data 1");
            
            await File.WriteAllTextAsync(Path.Combine(gitInSub2, "index"), "binary index data");
            Directory.CreateDirectory(Path.Combine(gitInSub2, "refs", "heads"));
            await File.WriteAllTextAsync(Path.Combine(gitInSub2, "refs", "heads", "master"), "abc123def456");
            
            // Create .git files in subdirectories (should be excluded)
            await File.WriteAllTextAsync(Path.Combine(testPath, "project", ".git"), "gitdir: /some/other/path/.git");
            await File.WriteAllTextAsync(Path.Combine(testPath, "vendor", ".git"), "gitdir: ../main/.git");
            
            // Create .git-like files that should NOT be excluded
            await File.WriteAllTextAsync(Path.Combine(subDir1, ".git_backup"), "backup data");
            await File.WriteAllTextAsync(Path.Combine(subDir2, ".gitkeep"), "keep this directory");
            
            // Create .git-like directories that should NOT be excluded
            var gitLikeDir = Path.Combine(testPath, "tools", ".git_hooks");
            Directory.CreateDirectory(gitLikeDir);
            await File.WriteAllTextAsync(Path.Combine(gitLikeDir, "pre-commit"), "#!/bin/bash");
            
            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);
            var status = await repository.GetWorkingDirectoryStatusAsync();
            
            var untrackedFiles = await repository.GetUntrackedFilesAsync(status);
            var untrackedFilePaths = untrackedFiles.Select(f => f.Path).ToArray();
            
            // Debug output
            Console.WriteLine($"Found {untrackedFilePaths.Length} untracked files:");
            foreach (var file in untrackedFilePaths.OrderBy(f => f))
            {
                Console.WriteLine($"  - {file}");
            }
            
            // Verify normal files are included
            Assert.IsTrue(untrackedFilePaths.Contains("project/src/main.cpp"), "Normal files should be included");
            Assert.IsTrue(untrackedFilePaths.Contains("vendor/lib/helper.js"), "Normal files should be included");
            
            // Verify .git directories and their contents are excluded
            Assert.IsFalse(untrackedFilePaths.Any(f => f.StartsWith("project/src/.git/")), "Subdirectory .git contents should be excluded");
            Assert.IsFalse(untrackedFilePaths.Any(f => f.StartsWith("vendor/lib/.git/")), "Subdirectory .git contents should be excluded");
            Assert.IsFalse(untrackedFilePaths.Contains("project/src/.git"), "Subdirectory .git directory should be excluded");
            Assert.IsFalse(untrackedFilePaths.Contains("vendor/lib/.git"), "Subdirectory .git directory should be excluded");
            
            // Verify .git files in subdirectories are excluded
            Assert.IsFalse(untrackedFilePaths.Contains("project/.git"), ".git file in subdirectory should be excluded");
            Assert.IsFalse(untrackedFilePaths.Contains("vendor/.git"), ".git file in subdirectory should be excluded");
            
            // Verify .git-like files are NOT excluded (should be included)
            Assert.IsTrue(untrackedFilePaths.Contains("project/src/.git_backup"), ".git_backup should be included");
            Assert.IsTrue(untrackedFilePaths.Contains("vendor/lib/.gitkeep"), ".gitkeep should be included");
            Assert.IsTrue(untrackedFilePaths.Contains("tools/.git_hooks/pre-commit"), ".git_hooks directory contents should be included");
            
            // Verify no .git paths are included at all
            var gitPaths = untrackedFilePaths.Where(f => 
                f.Contains("/.git/") || 
                f.EndsWith("/.git") || 
                f.StartsWith(".git/") ||
                f.Equals(".git")).ToArray();
            
            Assert.IsEmpty(gitPaths, $"No .git paths should be included, but found: {string.Join(", ", gitPaths)}");
        }
        finally
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }
} 