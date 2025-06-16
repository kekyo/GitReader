////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Collections;
using GitReader.Primitive;
using NUnit.Framework;

namespace GitReader.Internal;

[TestFixture]
public sealed class WorkingDirectoryAccessorConcurrencyTests
{
    private const int LargeFileCount = 500;
    private const int ConcurrentCallCount = 10;

    [Test]
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_WithManyFiles_ShouldHandleConcurrently()
    {
        // Test with a large number of files to verify parallel processing effectiveness
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_ParallelTest_{System.Guid.NewGuid():N}"));
        
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
            
            // Create many files to test parallel processing
            var fileTasks = new List<Task>();
            for (int i = 0; i < LargeFileCount; i++)
            {
                var fileName = $"test_file_{i:D4}.txt";
                var filePath = Path.Combine(testPath, fileName);
                fileTasks.Add(File.WriteAllTextAsync(filePath, $"Content of file {i}"));
            }
            await Task.WhenAll(fileTasks);

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Measure performance and verify correctness
            var stopwatch = Stopwatch.StartNew();
            var status = await repository.GetWorkingDirectoryStatusAsync();
            stopwatch.Stop();

            // Verify all files are detected as untracked
            Assert.AreEqual(LargeFileCount, status.UntrackedFiles.Count, 
                $"Should detect all {LargeFileCount} files as untracked");
            
            // Verify no duplicates in results
            var pathSet = new HashSet<string>();
            foreach (var file in status.UntrackedFiles)
            {
                Assert.IsTrue(pathSet.Add(file.Path), 
                    $"Duplicate file path detected: {file.Path}");
            }

            // Performance should benefit from parallelization
            Console.WriteLine($"Processed {LargeFileCount} files in {stopwatch.ElapsedMilliseconds}ms");
            
            // Verify result consistency - all files should be untracked with valid hashes
            foreach (var file in status.UntrackedFiles)
            {
                Assert.AreEqual(FileStatus.Untracked, (FileStatus)file.Status);
                Assert.IsNull(file.IndexHash, "Untracked files should not have index hash");
                Assert.IsNotNull(file.WorkingTreeHash, "Untracked files should have working tree hash");
            }
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
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_ConcurrentCalls_ShouldProduceSameResults()
    {
        // Test concurrent calls to the same repository
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_ConcurrentTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a Git repository with mixed file states
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create committed file
            await File.WriteAllTextAsync(Path.Combine(testPath, "committed.txt"), "Committed content");
            await TestUtilities.RunGitCommandAsync(testPath, "add committed.txt");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Wait a bit to ensure Git operations are complete
            await Task.Delay(100);
            
            // Create staged file
            await File.WriteAllTextAsync(Path.Combine(testPath, "staged.txt"), "Staged content");
            await TestUtilities.RunGitCommandAsync(testPath, "add staged.txt");
            
            // Wait to ensure staging is complete
            await Task.Delay(100);
            
            // Create untracked files
            for (int i = 0; i < 50; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(testPath, $"untracked_{i}.txt"), $"Untracked content {i}");
            }
            
            // Modify committed file
            await File.WriteAllTextAsync(Path.Combine(testPath, "committed.txt"), "Modified committed content");

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Execute multiple concurrent calls
            var tasks = new Task<PrimitiveWorkingDirectoryStatus>[ConcurrentCallCount];
            for (int i = 0; i < ConcurrentCallCount; i++)
            {
                tasks[i] = repository.GetWorkingDirectoryStatusAsync();
            }

            var results = await Task.WhenAll(tasks);

            // Verify all results are identical
            var firstResult = results[0];
            for (int i = 1; i < results.Length; i++)
            {
                var result = results[i];
                
                // Compare staged files
                Assert.AreEqual(firstResult.StagedFiles.Count, result.StagedFiles.Count, 
                    $"Staged files count mismatch in result {i}");
                AssertFilesEqual(firstResult.StagedFiles, result.StagedFiles, "staged");
                
                // Compare unstaged files
                Assert.AreEqual(firstResult.UnstagedFiles.Count, result.UnstagedFiles.Count, 
                    $"Unstaged files count mismatch in result {i}");
                AssertFilesEqual(firstResult.UnstagedFiles, result.UnstagedFiles, "unstaged");
                
                // Compare untracked files
                Assert.AreEqual(firstResult.UntrackedFiles.Count, result.UntrackedFiles.Count, 
                    $"Untracked files count mismatch in result {i}");
                AssertFilesEqual(firstResult.UntrackedFiles, result.UntrackedFiles, "untracked");
            }
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
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_ConcurrentCalls_ShouldNotInterfere()
    {
        // Test concurrent status checks without file modifications to isolate race conditions
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_ThreadSafeTest_{System.Guid.NewGuid():N}"));
        
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
            
            // Create stable set of files
            for (int i = 0; i < 50; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(testPath, $"file_{i}.txt"), $"Content {i}");
            }

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Execute multiple concurrent calls without cancellation token conflicts
            var concurrentTasks = new List<Task<PrimitiveWorkingDirectoryStatus>>();
            for (int i = 0; i < 10; i++)
            {
                concurrentTasks.Add(Task.Run(async () =>
                {
                    // Each task gets its own cancellation token to avoid interference
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    return await repository.GetWorkingDirectoryStatusAsync(cts.Token);
                }));
            }

            var results = await Task.WhenAll(concurrentTasks);
            
            // Verify all results are identical
            for (int i = 1; i < results.Length; i++)
            {
                Assert.AreEqual(results[0].UntrackedFiles.Count, results[i].UntrackedFiles.Count,
                    $"Untracked files count should be consistent across concurrent calls");
                Assert.AreEqual(results[0].StagedFiles.Count, results[i].StagedFiles.Count,
                    $"Staged files count should be consistent across concurrent calls");
                Assert.AreEqual(results[0].UnstagedFiles.Count, results[i].UnstagedFiles.Count,
                    $"Unstaged files count should be consistent across concurrent calls");
            }
            
            // Verify the actual content consistency
            var firstResult = results[0];
            foreach (var result in results.Skip(1))
            {
                AssertFilesEqual(firstResult.UntrackedFiles, result.UntrackedFiles, "untracked");
                AssertFilesEqual(firstResult.StagedFiles, result.StagedFiles, "staged");
                AssertFilesEqual(firstResult.UnstagedFiles, result.UnstagedFiles, "unstaged");
            }
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
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_ParallelFileProcessing_ShouldBeConsistent()
    {
        // Test that parallel file processing maintains consistency
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_ParallelConsistencyTest_{System.Guid.NewGuid():N}"));
        
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
            
            // Create a complex file structure to test parallel processing edge cases
            var directories = new[] { "dir1", "dir2", "dir3", "dir1/subdir", "dir2/subdir" };
            foreach (var dir in directories)
            {
                Directory.CreateDirectory(Path.Combine(testPath, dir));
            }
            
            // Create files with various states
            var files = new Dictionary<string, string>
            {
                { "root.txt", "Root file content" },
                { "dir1/file1.txt", "Dir1 file content" },
                { "dir1/subdir/nested.txt", "Nested file content" },
                { "dir2/file2.txt", "Dir2 file content" },
                { "dir2/subdir/another.txt", "Another nested file" },
                { "dir3/file3.txt", "Dir3 file content" }
            };
            
            foreach (var file in files)
            {
                await File.WriteAllTextAsync(Path.Combine(testPath, file.Key), file.Value);
            }
            
            // Stage some files to create mixed states
            await TestUtilities.RunGitCommandAsync(testPath, "add root.txt dir1/file1.txt");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Add some files to staging area
            await TestUtilities.RunGitCommandAsync(testPath, "add dir2/file2.txt");
            
            // Modify a committed file
            await File.WriteAllTextAsync(Path.Combine(testPath, "root.txt"), "Modified root content");

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            // Run the same operation multiple times to check for consistency
            var results = new List<PrimitiveWorkingDirectoryStatus>();
            for (int i = 0; i < 5; i++)
            {
                results.Add(await repository.GetWorkingDirectoryStatusAsync());
            }

            // Verify all results are identical
            var firstResult = results[0];
            foreach (var result in results.Skip(1))
            {
                Assert.AreEqual(firstResult.StagedFiles.Count, result.StagedFiles.Count, "Staged files count should be consistent");
                Assert.AreEqual(firstResult.UnstagedFiles.Count, result.UnstagedFiles.Count, "Unstaged files count should be consistent");
                Assert.AreEqual(firstResult.UntrackedFiles.Count, result.UntrackedFiles.Count, "Untracked files count should be consistent");
                
                AssertFilesEqual(firstResult.StagedFiles, result.StagedFiles, "staged");
                AssertFilesEqual(firstResult.UnstagedFiles, result.UnstagedFiles, "unstaged");
                AssertFilesEqual(firstResult.UntrackedFiles, result.UntrackedFiles, "untracked");
            }
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
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_IndexFileSharedRead_ShouldBeThreadSafe()
    {
        // Test that multiple concurrent reads of the same index file don't interfere
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_SharedReadTest_{System.Guid.NewGuid():N}"));
        
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, true);
        }
        
        try
        {
            Directory.CreateDirectory(testPath);
            
            // Create a stable Git repository
            await TestUtilities.RunGitCommandAsync(testPath, "init");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.email \"test@example.com\"");
            await TestUtilities.RunGitCommandAsync(testPath, "config user.name \"Test User\"");
            
            // Create stable files
            for (int i = 0; i < 100; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(testPath, $"file_{i}.txt"), $"Content {i}");
            }
            await TestUtilities.RunGitCommandAsync(testPath, "add .");
            await TestUtilities.RunGitCommandAsync(testPath, "commit -m \"Initial commit\"");
            
            // Ensure Git operations are complete
            await Task.Delay(200);

            // Open multiple repository instances to simulate real concurrent access
            var repositories = new List<PrimitiveRepository>();
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    repositories.Add(await Repository.Factory.OpenPrimitiveAsync(testPath));
                }

                // Execute concurrent calls on different repository instances
                var concurrentTasks = repositories.Select(repo => 
                    Task.Run(async () =>
                    {
                        // Each task reads the same index file multiple times
                        var results = new List<PrimitiveWorkingDirectoryStatus>();
                        for (int i = 0; i < 3; i++)
                        {
                            results.Add(await repo.GetWorkingDirectoryStatusAsync());
                            await Task.Delay(10); // Small delay to create timing variance
                        }
                        return results;
                    })).ToList();

                var allResults = await Task.WhenAll(concurrentTasks);

                // Verify all results are consistent
                var firstResult = allResults[0][0];
                foreach (var taskResults in allResults)
                {
                    foreach (var result in taskResults)
                    {
                        Assert.AreEqual(firstResult.StagedFiles.Count, result.StagedFiles.Count,
                            "All concurrent reads should return same staged file count");
                        Assert.AreEqual(firstResult.UnstagedFiles.Count, result.UnstagedFiles.Count,
                            "All concurrent reads should return same unstaged file count");
                        Assert.AreEqual(firstResult.UntrackedFiles.Count, result.UntrackedFiles.Count,
                            "All concurrent reads should return same untracked file count");
                    }
                }
            }
            finally
            {
                foreach (var repo in repositories)
                {
                    repo.Dispose();
                }
            }
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
    public async Task GetPrimitiveWorkingDirectoryStatusAsync_ProcessedPathsConsistency_ShouldHaveNoDuplicates()
    {
        // Test that processedPaths doesn't have race conditions leading to duplicates or missing entries
        var testPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"GitReader_ProcessedPathsTest_{System.Guid.NewGuid():N}"));
        
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
            
            // Create files with similar names to test path collision scenarios
            var similarPaths = new[]
            {
                "file.txt", "file_1.txt", "file_2.txt",
                "dir/file.txt", "dir/file_1.txt", "dir/file_2.txt",
                "dir_1/file.txt", "dir_2/file.txt"
            };
            
            foreach (var path in similarPaths)
            {
                var fullPath = Path.Combine(testPath, path);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(fullPath, $"Content for {path}");
            }
            
            // Stage some files to create index entries
            await TestUtilities.RunGitCommandAsync(testPath, "add file.txt dir/file.txt");

            using var repository = await Repository.Factory.OpenPrimitiveAsync(testPath);

            var status = await repository.GetWorkingDirectoryStatusAsync();

            // Verify all paths are unique across all collections
            var allPaths = new List<string>();
            allPaths.AddRange(status.StagedFiles.Select(f => f.Path));
            allPaths.AddRange(status.UnstagedFiles.Select(f => f.Path));
            allPaths.AddRange(status.UntrackedFiles.Select(f => f.Path));
            
            var pathSet = new HashSet<string>();
            foreach (var path in allPaths)
            {
                Assert.IsTrue(pathSet.Add(path), $"Duplicate path found: {path}");
            }
            
            // Verify expected files are categorized correctly
            Assert.IsTrue(status.StagedFiles.Any(f => f.Path == "file.txt"), "file.txt should be staged");
            Assert.IsTrue(status.StagedFiles.Any(f => f.Path == "dir/file.txt"), "dir/file.txt should be staged");
            
            // All other files should be untracked
            var expectedUntrackedCount = similarPaths.Length - 2; // minus the 2 staged files
            Assert.AreEqual(expectedUntrackedCount, status.UntrackedFiles.Count, 
                "Unexpected number of untracked files");
        }
        finally
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, true);
            }
        }
    }

    private static void AssertFilesEqual(
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> expected,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> actual,
        string category)
    {
        var expectedDict = expected.ToDictionary(f => f.Path);
        var actualDict = actual.ToDictionary(f => f.Path);
        
        foreach (var expectedFile in expectedDict)
        {
            Assert.IsTrue(actualDict.TryGetValue(expectedFile.Key, out var actualFile), 
                $"Missing {category} file: {expectedFile.Key}");
            
            Assert.AreEqual(expectedFile.Value.Status, actualFile.Status, 
                $"Status mismatch for {category} file: {expectedFile.Key}");
            
            Assert.AreEqual(expectedFile.Value.IndexHash, actualFile.IndexHash, 
                $"IndexHash mismatch for {category} file: {expectedFile.Key}");
            
            Assert.AreEqual(expectedFile.Value.WorkingTreeHash, actualFile.WorkingTreeHash, 
                $"WorkingTreeHash mismatch for {category} file: {expectedFile.Key}");
        }
        
        foreach (var actualFile in actualDict)
        {
            Assert.IsTrue(expectedDict.ContainsKey(actualFile.Key), 
                $"Unexpected {category} file: {actualFile.Key}");
        }
    }
} 
