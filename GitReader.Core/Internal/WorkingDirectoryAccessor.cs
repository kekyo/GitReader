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

        // Get the current commit's tree for comparison
        var headReference = await PrimitiveRepositoryFacade.GetCurrentHeadReferenceAsync(repository, ct);
        var headTreeFiles = new Dictionary<string, Hash>();
        
        if (headReference != null)
        {
            var headCommit = await RepositoryAccessor.ReadCommitAsync(repository, headReference.Value.Target, ct);
            if (headCommit != null)
            {
                // Build dictionary of files from HEAD commit tree
                await BuildTreeFileDictionaryAsync(repository, headCommit.Value.TreeRoot, "", headTreeFiles, ct);
            }
        }

        // Process index entries
        foreach (var indexEntry in indexFileDict.Values)
        {
            var workingFilePath = repository.fileSystem.Combine(workingDirectoryPath, indexEntry.Path);
            processedPaths.Add(indexEntry.Path);

            if (await repository.fileSystem.IsFileExistsAsync(workingFilePath, ct))
            {
                // File exists in working directory
                var workingHash = await CalculateFileHashAsync(repository, workingFilePath, ct).ConfigureAwait(false);
                
                // Check if this file exists in HEAD commit
                var isInHeadCommit = headTreeFiles.TryGetValue(indexEntry.Path, out var headFileHash);
                
                if (workingHash.Equals(indexEntry.ObjectHash))
                {
                    if (isInHeadCommit && indexEntry.ObjectHash.Equals(headFileHash))
                    {
                        // File is committed and unmodified - don't include in any list
                        // This matches git status behavior for clean repositories
                    }
                    else
                    {
                        // File is staged (added to index but not yet committed or modified since commit)
                        stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            isInHeadCommit ? FileStatus.Modified : FileStatus.Added,
                            isInHeadCommit ? (Hash?)headFileHash : null,
                            workingHash));
                    }
                }
                else
                {
                    // File content differs between index and working directory
                    if (isInHeadCommit)
                    {
                        if (!indexEntry.ObjectHash.Equals(headFileHash))
                        {
                            // File is staged (modified in index compared to HEAD)
                            stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                                indexEntry.Path,
                                FileStatus.Modified,
                                headFileHash,
                                indexEntry.ObjectHash));
                        }
                        
                        // File is also modified in working directory
                        unstagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            FileStatus.Modified,
                            indexEntry.ObjectHash,
                            workingHash));
                    }
                    else
                    {
                        // New file: staged but modified in working directory
                        stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            FileStatus.Added,
                            null,
                            indexEntry.ObjectHash));
                        
                        unstagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            FileStatus.Modified,
                            indexEntry.ObjectHash,
                            workingHash));
                    }
                }
            }
            else
            {
                // File is in index but deleted in working directory
                var isInHeadCommit = headTreeFiles.TryGetValue(indexEntry.Path, out var headFileHash);
                
                if (isInHeadCommit && !indexEntry.ObjectHash.Equals(headFileHash))
                {
                    // File was staged (modified in index) but then deleted in working directory
                    stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                        indexEntry.Path,
                        FileStatus.Modified,
                        headFileHash,
                        indexEntry.ObjectHash));
                }
                else if (!isInHeadCommit)
                {
                    // File was staged (newly added) but then deleted in working directory
                    stagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                        indexEntry.Path,
                        FileStatus.Added,
                        null,
                        indexEntry.ObjectHash));
                }
                
                // File is deleted in working directory
                unstagedFiles.Add(new PrimitiveWorkingDirectoryFile(
                    indexEntry.Path,
                    FileStatus.Deleted,
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
    private static async ValueTask BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash, string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#else
    private static async Task BuildTreeFileDictionaryAsync(
        Repository repository, Hash treeHash, string basePath, Dictionary<string, Hash> fileDict, CancellationToken ct)
#endif
    {
        var tree = await RepositoryAccessor.ReadTreeAsync(repository, treeHash, ct);
        
        foreach (var entry in tree.Children)
        {
            var fullPath = string.IsNullOrEmpty(basePath) ? entry.Name : basePath + "/" + entry.Name;
            
            if (entry.SpecialModes == PrimitiveSpecialModes.Tree || entry.SpecialModes == PrimitiveSpecialModes.Directory)
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