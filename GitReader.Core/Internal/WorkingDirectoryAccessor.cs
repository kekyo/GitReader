////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using GitReader.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class WorkingDirectoryAccessor
{
    /// <summary>
    /// Traverse context.
    /// </summary>
    /// <remarks>
    /// This class is used to limit the number of parallel tasks.
    /// This is a very loose way to limit the number of parallel tasks.
    /// </remarks>
    private sealed class TraverseContext
    {
        public readonly Repository Repository;
        public readonly string WorkingDirectoryPath;
        public readonly HashSet<string> ProcessedPaths;
        public readonly GlobFilter OverrideGlobFilter;
        public readonly List<PrimitiveWorkingDirectoryFile> UntrackedFiles;
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="repository">The repository to scan.</param>
        /// <param name="workingDirectoryPath">The path to the working directory.</param>
        /// <param name="processedPaths">The paths that have already been processed.</param>
        /// <param name="overrideGlobFilter">The override glob filter.</param>
        /// <param name="untrackedFiles">The list of untracked files (output)</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public TraverseContext(
            Repository repository,
            string workingDirectoryPath,
            HashSet<string> processedPaths,
            GlobFilter overrideGlobFilter,
            List<PrimitiveWorkingDirectoryFile> untrackedFiles,
            CancellationToken cancellationToken)
            {
                this.Repository = repository;
                this.WorkingDirectoryPath = workingDirectoryPath;
                this.ProcessedPaths = processedPaths;
                this.OverrideGlobFilter = overrideGlobFilter;
                this.UntrackedFiles = untrackedFiles;
                this.CancellationToken = cancellationToken;
            }
    }

    /// <summary>
    /// Scans the working directory for untracked files recursively.
    /// </summary>
    /// <param name="context">Traverse context</param>
    /// <param name="currentPath">The current path to scan.</param>
    /// <param name="parentGlobFilter">The parent glob filter.</param>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    private static async ValueTask ExtractUntrackedFilesRecursiveAsync(
        TraverseContext context,
        string currentPath,
        GlobFilter parentGlobFilter)
#else
    private static async Task ExtractUntrackedFilesRecursiveAsync(
        TraverseContext context,
        string currentPath,
        GlobFilter parentGlobFilter)
#endif
    {
        // Skip .git directory/file (hardcoded exclusion, same as Git official behavior)
        var currentName = context.Repository.fileSystem.GetFileName(currentPath);
        if (currentName.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (!await context.Repository.fileSystem.IsDirectoryExistsAsync(currentPath, context.CancellationToken))
            {
                return;
            }

            // Read .gitignore in current directory and combine with pathFilter.
            GlobFilter candidateGlobFilter;
            var gitignorePath = context.Repository.fileSystem.Combine(currentPath, ".gitignore");
            try
            {
                // When .gitignore exists
                if (await context.Repository.fileSystem.IsFileExistsAsync(gitignorePath, context.CancellationToken))
                {
                    // Generate .gitignore filter
                    using var gitignoreStream = await context.Repository.fileSystem.OpenAsync(gitignorePath, false, context.CancellationToken);
                    var gitignoreFilter = await Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, context.CancellationToken);

                    // Combine filters with correct order: parent filter, .gitignore filter, override filter
                    candidateGlobFilter = Glob.Combine([parentGlobFilter, gitignoreFilter]);
                }
                else
                {
                    // When .gitignore does not exist, continue with parent filter
                    candidateGlobFilter = parentGlobFilter;
                }
            }
            catch
            {
                // If .gitignore cannot be read, continue with parent filter
                candidateGlobFilter = parentGlobFilter;
            }
            
            // Scan directory entries
            var entries = await context.Repository.fileSystem.GetDirectoryEntriesAsync(currentPath, context.CancellationToken);

            // Makes sub tasks iterator
            await context.Repository.concurrentScope.WhenAll(
                entries.Select((async entry =>
            {
                // Skip .git directory/files (hardcoded exclusion matching Git's behavior)
                var fileName = context.Repository.fileSystem.GetFileName(entry);
                if (fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Get relative path and filter it
                var relativePath = context.Repository.fileSystem.ToPosixPath(
                    context.Repository.fileSystem.GetRelativePath(context.WorkingDirectoryPath, entry));

                var exactlyPathFilter = Glob.Combine([candidateGlobFilter, context.OverrideGlobFilter]);
                var filterResult = Glob.ApplyFilter(exactlyPathFilter, relativePath);

                // When entry is excluded, ignore it.
                if (filterResult == GlobFilterStates.Exclude)
                {
                    return;
                }

                // When entry is a directory
                if (await context.Repository.fileSystem.IsDirectoryExistsAsync(entry, context.CancellationToken))
                {
                    // Recursively scan subdirectories with the current candidate filter
                    await ExtractUntrackedFilesRecursiveAsync(
                        context, entry, candidateGlobFilter);
                }
                // When entry is a file
                else if (await context.Repository.fileSystem.IsFileExistsAsync(entry, context.CancellationToken))
                {
                    // When entry is a file, add it to untracked files if it is not processed yet
                    if (!context.ProcessedPaths.Contains(relativePath))
                    {
                        // This is an untracked file that passes the filter
                        var fileHash = await CalculateFileHashAsync(context.Repository, entry, context.CancellationToken);

                        var untrackedFile = new PrimitiveWorkingDirectoryFile(
                            relativePath,
                            FileStatus.Untracked,
                            null,
                            fileHash);

                        // Avoid race condition
                        lock (context.UntrackedFiles)
                        {
                            context.UntrackedFiles.Add(untrackedFile);
                        }
                    }
                }
            })));
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible directories
        }
        catch (Exception)
        {
            // Skip directories that cannot be accessed for any reason
        }
    }

    /// <summary>
    /// Scans the working directory for untracked files.
    /// </summary>
    /// <param name="repository">The repository to scan.</param>
    /// <param name="workingDirectoryPath">The path to the working directory.</param>
    /// <param name="processedPaths">The paths that have already been processed.</param>
    /// <param name="overrideGlobFilter">The override glob filter.</param>
    /// <param name="untrackedFiles">The list of untracked files (output)</param>
    /// <param name="ct">The cancellation token.</param>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static ValueTask ExtractUntrackedFilesAsync(
        Repository repository,
        string workingDirectoryPath,
        HashSet<string> processedPaths,
        GlobFilter overrideGlobFilter,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        CancellationToken ct) =>
#else
    public static Task ExtractUntrackedFilesAsync(
        Repository repository,
        string workingDirectoryPath,
        HashSet<string> processedPaths,
        GlobFilter overrideGlobFilter,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        CancellationToken ct) =>
#endif
        ExtractUntrackedFilesRecursiveAsync(
            new TraverseContext(
                repository,
                workingDirectoryPath,
                processedPaths,
                overrideGlobFilter,
                untrackedFiles,
                ct),
            workingDirectoryPath,
            // Initial glob filter (always nothing)
            Glob.nothingFilter);

    /// <summary>
    /// Builds a dictionary of file paths and their hashes from a tree.
    /// </summary>
    /// <param name="repository">The repository to scan.</param>
    /// <param name="treeHash">The hash of the tree to scan.</param>
    /// <param name="basePath">The base path to use for relative paths.</param>
    /// <param name="fileDict">The dictionary to store the file paths and their hashes.</param>
    /// <param name="ct">The cancellation token.</param>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash,
        string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#else
    public static async Task BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash,
        string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#endif
    {
        // Read the tree
        var tree = await RepositoryAccessor.ReadTreeAsync(repository, treeHash, ct);

        // Iterate over the tree entries
        await repository.concurrentScope.WhenAll(ct,
            tree.Children.Select((async entry =>
            {
                var fullPath = string.IsNullOrEmpty(basePath) ? entry.Name : basePath + "/" + entry.Name;

                if (entry.SpecialModes is PrimitiveSpecialModes.Tree or PrimitiveSpecialModes.Directory)
                {
                    // Recursively process subdirectories
                    await BuildTreeFileDictionaryAsync(repository, entry.Hash, fullPath, fileDict, ct);
                }
                else
                {
                    // Avoid race condition
                    lock (fileDict)
                    {
                        // Add file to dictionary
                        fileDict[fullPath] = entry.Hash;
                    }
                }
            })));
    }

    /// <summary>
    /// Calculates the hash of a file.
    /// </summary>
    /// <param name="repository">The repository to scan.</param>
    /// <param name="filePath">The path to the file to calculate the hash of.</param>
    /// <param name="ct">The cancellation token.</param>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<Hash> CalculateFileHashAsync(
        Repository repository, string filePath, CancellationToken ct)
#else
    public static async Task<Hash> CalculateFileHashAsync(
        Repository repository, string filePath, CancellationToken ct)
#endif
    {
        using var fs = await repository.fileSystem.OpenAsync(filePath, false, ct);
        return await Utilities.CalculateGitBlobHashAsync(fs, fs.Length, repository.pool, ct);
    }
} 