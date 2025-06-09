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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class WorkingDirectoryAccessor
{
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct)
#else
    public static async Task<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct)
#endif
    {
        // Get staged files from index
        var indexEntries = await GitIndexReader.ReadIndexEntriesAsync(repository, ct);
        var indexFileDict = indexEntries
            .Where(entry => entry.IsValidFlag && !entry.IsStageFlag)
            .ToDictionary(entry => entry.Path, entry => entry);

        // Get working directory path
        var workingDirectoryPath = repository.fileSystem.GetDirectoryPath(repository.GitPath);

        // Collect file lists
        var stagedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var unstagedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var untrackedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var processedPaths = new HashSet<string>();

        // Process index entries
        foreach (var indexEntry in indexFileDict.Values)
        {
            var workingFilePath = repository.fileSystem.Combine(workingDirectoryPath, indexEntry.Path);
            processedPaths.Add(indexEntry.Path);

            if (await repository.fileSystem.IsFileExistsAsync(workingFilePath, ct))
            {
                // File exists in working directory
                var workingHash = await CalculateFileHashAsync(repository, workingFilePath, ct).ConfigureAwait(false);
                
                if (workingHash.Equals(indexEntry.ObjectHash))
                {
                    // File is staged and unmodified
                    stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                        indexEntry.Path,
                        PrimitiveFileStatus.Unmodified,
                        indexEntry.ObjectHash,
                        workingHash));
                }
                else
                {
                    // File is staged but modified in working directory
                    stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                        indexEntry.Path,
                        PrimitiveFileStatus.Added,
                        indexEntry.ObjectHash,
                        null));
                    
                    unstagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                        indexEntry.Path,
                        PrimitiveFileStatus.Modified,
                        indexEntry.ObjectHash,
                        workingHash));
                }
            }
            else
            {
                // File is staged but deleted in working directory
                stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                    indexEntry.Path,
                    PrimitiveFileStatus.Added,
                    indexEntry.ObjectHash,
                    null));
                
                unstagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                    indexEntry.Path,
                    PrimitiveFileStatus.Deleted,
                    indexEntry.ObjectHash,
                    null));
            }
        }

        // Find untracked files in working directory
        await ScanWorkingDirectoryAsync(
            repository, workingDirectoryPath, workingDirectoryPath, 
            processedPaths, untrackedFiles, ct).ConfigureAwait(false);

        return new PrimitiveWorkingDirectoryStatus(
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(stagedFiles.ToArray()),
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(unstagedFiles.ToArray()),
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(untrackedFiles.ToArray()));
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct)
#else
    public static async Task<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct)
#endif
    {
        var primitiveStatus = await GetPrimitiveWorkingDirectoryStatusAsync(repository, ct);

        var stagedFiles = primitiveStatus.StagedFiles.Select(pf => new WorkingDirectoryFile(
            rwr, pf.Path, (FileStatus)pf.Status, pf.IndexHash, pf.WorkingTreeHash)).ToArray();
        
        var unstagedFiles = primitiveStatus.UnstagedFiles.Select(pf => new WorkingDirectoryFile(
            rwr, pf.Path, (FileStatus)pf.Status, pf.IndexHash, pf.WorkingTreeHash)).ToArray();
        
        var untrackedFiles = primitiveStatus.UntrackedFiles.Select(pf => new WorkingDirectoryFile(
            rwr, pf.Path, (FileStatus)pf.Status, pf.IndexHash, pf.WorkingTreeHash)).ToArray();

        return new WorkingDirectoryStatus(
            new ReadOnlyArray<WorkingDirectoryFile>(stagedFiles),
            new ReadOnlyArray<WorkingDirectoryFile>(unstagedFiles),
            new ReadOnlyArray<WorkingDirectoryFile>(untrackedFiles));
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    private static async ValueTask ScanWorkingDirectoryAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        CancellationToken ct)
#else
    private static async Task ScanWorkingDirectoryAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        CancellationToken ct)
#endif
    {
        // Skip .git directory
        if (repository.fileSystem.GetFileName(currentPath).Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (!await repository.fileSystem.IsDirectoryExistsAsync(currentPath, ct))
            {
                return;
            }

            var entries = await repository.fileSystem.GetDirectoryEntriesAsync(currentPath, ct);
            
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();

                if (await repository.fileSystem.IsDirectoryExistsAsync(entry, ct))
                {
                    // Recursively scan subdirectories
                    await ScanWorkingDirectoryAsync(
                        repository, workingDirectoryPath, entry, processedPaths, untrackedFiles, ct).ConfigureAwait(false);
                }
                else if (await repository.fileSystem.IsFileExistsAsync(entry, ct))
                {
                    // Get relative path from working directory root
                    var relativePath = repository.fileSystem.GetRelativePath(workingDirectoryPath, entry);
                    
                    if (!processedPaths.Contains(relativePath))
                    {
                        // This is an untracked file
                        var fileHash = await CalculateFileHashAsync(repository, entry, ct).ConfigureAwait(false);
                        
                        untrackedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            relativePath,
                            PrimitiveFileStatus.Untracked,
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
    private static async ValueTask<Hash> CalculateFileHashAsync(
        Repository repository, string filePath, CancellationToken ct)
#else
    private static async Task<Hash> CalculateFileHashAsync(
        Repository repository, string filePath, CancellationToken ct)
#endif
    {
        using var fs = await repository.fileSystem.OpenAsync(filePath, false, ct);
        return await Utilities.CalculateGitBlobHashAsync(fs, fs.Length, repository.pool, ct);
    }
} 