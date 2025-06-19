////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.IO;
using GitReader.Primitive;
using GitReader.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class WorkingDirectoryAccessor
{
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask ScanWorkingDirectoryAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        GlobFilter overrideGlobFilter,
        GlobFilter parentPathFilter,
        CancellationToken ct)
#else
    public static async Task ScanWorkingDirectoryAsync(
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
                    await ScanWorkingDirectoryAsync(
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash, string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#else
    public static async Task BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash, string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#endif
    {
        var tree = await RepositoryAccessor.ReadTreeAsync(repository, treeHash, ct);
        
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