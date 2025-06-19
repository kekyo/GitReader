////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class WorkingDirectoryAccessor
{
    /// <summary>
    /// Scans the working directory for untracked files.
    /// </summary>
    /// <param name="repository">The repository to scan.</param>
    /// <param name="workingDirectoryPath">The path to the working directory.</param>
    /// <param name="currentPath">The current path to scan.</param>
    /// <param name="processedPaths">The paths that have already been processed.</param>
    /// <param name="untrackedFiles">The list of untracked files (output)</param>
    /// <param name="overrideGlobFilter">The override glob filter.</param>
    /// <param name="parentPathFilter">The parent path filter.</param>
    /// <param name="ct">The cancellation token.</param>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask ScanWorkingDirectoryRecursiveAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        GlobFilter overrideGlobFilter,
        GlobFilter parentPathFilter,
        CancellationToken ct)
#else
    public static async Task ScanWorkingDirectoryRecursiveAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        GlobFilter overrideGlobFilter,
        GlobFilter parentPathFilter,
        CancellationToken ct)
#endif
    {
        // Skip .git directory/file (hardcoded exclusion, same as Git official behavior)
        var currentName = repository.fileSystem.GetFileName(currentPath);
        if (currentName.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (!await repository.fileSystem.IsDirectoryExistsAsync(currentPath, ct))
            {
                return;
            }

            // Read .gitignore in current directory and combine with pathFilter.
            GlobFilter candidatePathFilter;
            GlobFilter exactlyPathFilter;
            var gitignorePath = repository.fileSystem.Combine(currentPath, ".gitignore");
            try
            {
                // When .gitignore exists
                if (await repository.fileSystem.IsFileExistsAsync(gitignorePath, ct))
                {
                    // Generate .gitignore filter
                    using var gitignoreStream = await repository.fileSystem.OpenAsync(gitignorePath, false, ct);
                    var gitignoreFilter = await Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, ct);

                    // Combine filters with correct order: parent filter, .gitignore filter, override filter
                    candidatePathFilter = Glob.Combine([ parentPathFilter, gitignoreFilter ]);
                    exactlyPathFilter = Glob.Combine([ parentPathFilter, gitignoreFilter, overrideGlobFilter ]);
                }
                else
                {
                    // When .gitignore does not exist, continue with parent filter
                    candidatePathFilter = parentPathFilter;
                    exactlyPathFilter = Glob.Combine([ parentPathFilter, overrideGlobFilter ]);
                }
            }
            catch
            {
                // If .gitignore cannot be read, continue with parent filter
                candidatePathFilter = parentPathFilter;
                exactlyPathFilter = Glob.Combine([ parentPathFilter, overrideGlobFilter ]);
            }

            // Scan directory entries
            var entries = await repository.fileSystem.GetDirectoryEntriesAsync(currentPath, ct);
            foreach (var entry in entries)
            {
                // Skip .git directory/files (hardcoded exclusion matching Git's behavior)
                var fileName = repository.fileSystem.GetFileName(entry);
                if (fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Get relative path and filter it
                var relativePath = repository.fileSystem.ToPosixPath(
                    repository.fileSystem.GetRelativePath(workingDirectoryPath, entry));
                var filterDecision = exactlyPathFilter(
                    GlobFilterStates.NotExclude,   // Start from neutral.
                    relativePath);

                // When entry is excluded, ignore it.
                if (filterDecision == GlobFilterStates.Exclude)
                {
                    continue;
                }

                // When entry is a directory
                if (await repository.fileSystem.IsDirectoryExistsAsync(entry, ct))
                {
                    // Recursively scan subdirectories with the current candidate filter
                    await ScanWorkingDirectoryRecursiveAsync(
                        repository, workingDirectoryPath, entry,
                        processedPaths, untrackedFiles,
                        overrideGlobFilter, candidatePathFilter,
                        ct);
                }
                // When entry is a file
                else if (await repository.fileSystem.IsFileExistsAsync(entry, ct))
                {
                    // When entry is a file, add it to untracked files if it is not processed yet
                    if (!processedPaths.Contains(relativePath))
                    {
                        // This is an untracked file that passes the filter
                        var fileHash = await CalculateFileHashAsync(repository, entry, ct);

                        untrackedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            relativePath,
                            FileStatus.Untracked,
                            null,
                            fileHash));
                    }
                }
            }
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
        foreach (var entry in tree.Children)
        {
            var fullPath = string.IsNullOrEmpty(basePath) ? entry.Name : basePath + "/" + entry.Name;
            
            if (entry.SpecialModes is PrimitiveSpecialModes.Tree or PrimitiveSpecialModes.Directory)
            {
                // Recursively process subdirectories
                await BuildTreeFileDictionaryAsync(repository, entry.Hash, fullPath, fileDict, ct);
            }
            else
            {
                // Add file to dictionary
                fileDict[fullPath] = entry.Hash;
            }
        }
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