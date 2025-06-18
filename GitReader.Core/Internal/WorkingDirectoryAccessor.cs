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
    /// <summary>
    /// Gets primitive working directory status information for the specified repository.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A ValueTask containing the primitive working directory status.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static ValueTask<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct) =>
        GetPrimitiveWorkingDirectoryStatusWithFilterAsync(repository, Glob.includeAll, ct);
#else
    public static Task<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct) =>
        GetPrimitiveWorkingDirectoryStatusWithFilterAsync(repository, Glob.includeAll, ct);
#endif

    private readonly struct Status
    {
        private readonly PrimitiveWorkingDirectoryFile? StagedFile;
        private readonly PrimitiveWorkingDirectoryFile? UnstagedFile;
        private readonly string ProcessedPath;

        public Status(
            PrimitiveWorkingDirectoryFile? stagedFile,
            PrimitiveWorkingDirectoryFile? unstagedFile,
            string processedPath)
        {
            this.StagedFile = stagedFile;
            this.UnstagedFile = unstagedFile;
            this.ProcessedPath = processedPath;
        }

        public void Deconstruct(
            out PrimitiveWorkingDirectoryFile? stagedFile,
            out PrimitiveWorkingDirectoryFile? unstagedFile,
            out string processedPath)
        {
            stagedFile = this.StagedFile;
            unstagedFile = this.UnstagedFile;
            processedPath = this.ProcessedPath;
        }
    }

    /// <summary>
    /// Gets primitive working directory status information for the specified repository with file filtering.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="pathFilter">Predicate to filter files by path.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A ValueTask containing the primitive working directory status.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusWithFilterAsync(
        Repository repository, Func<string, bool> pathFilter, CancellationToken ct)
#else
    public static async Task<PrimitiveWorkingDirectoryStatus> GetPrimitiveWorkingDirectoryStatusWithFilterAsync(
        Repository repository, Func<string, bool> pathFilter, CancellationToken ct)
#endif
    {
        // Get staged files from index
        var indexEntries = await GitIndexReader.ReadIndexEntriesAsync(repository, ct);
        var indexFileDict = indexEntries
            .Where(entry => entry is { IsValidFlag: true, IsStageFlag: false })
            .ToDictionary(entry => entry.Path, entry => entry);

        // Get working directory path
        var workingDirectoryPath = repository.fileSystem.GetDirectoryPath(repository.GitPath);

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

        // Process index entries in parallel
        var results = await Utilities.WhenAll(
            indexFileDict.Values.Select(async indexEntry =>
            {
                var stagedFile = (PrimitiveWorkingDirectoryFile?)null;
                var unstagedFile = (PrimitiveWorkingDirectoryFile?)null;
                var processedPath = indexEntry.Path;

                // Apply path filter to index entries
                if (!pathFilter(indexEntry.Path))
                {
                    return new Status(stagedFile, unstagedFile, processedPath);
                }

                var workingFilePath = repository.fileSystem.Combine(workingDirectoryPath, indexEntry.Path);
                
                if (await repository.fileSystem.IsFileExistsAsync(workingFilePath, ct))
                {
                    // File exists in working directory
                    var workingHash = await CalculateFileHashAsync(repository, workingFilePath, ct);
                    
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
                            stagedFile = new PrimitiveWorkingDirectoryFile(
                                indexEntry.Path,
                                isInHeadCommit ? FileStatus.Modified : FileStatus.Added,
                                isInHeadCommit ? (Hash?)headFileHash : null,
                                workingHash);
                        }
                    }
                    else
                    {
                        // File has been modified since last index update
                        if (isInHeadCommit && indexEntry.ObjectHash.Equals(headFileHash))
                        {
                            // File was not staged but has been modified
                            unstagedFile = new PrimitiveWorkingDirectoryFile(
                                indexEntry.Path,
                                FileStatus.Modified,
                                indexEntry.ObjectHash,
                                workingHash);
                        }
                        else
                        {
                            // File is staged and also has unstaged changes
                            stagedFile = new PrimitiveWorkingDirectoryFile(
                                indexEntry.Path,
                                isInHeadCommit ? FileStatus.Modified : FileStatus.Added,
                                isInHeadCommit ? (Hash?)headFileHash : null,
                                indexEntry.ObjectHash);
                            
                            unstagedFile = new PrimitiveWorkingDirectoryFile(
                                indexEntry.Path,
                                FileStatus.Modified,
                                indexEntry.ObjectHash,
                                workingHash);
                        }
                    }
                }
                else
                {
                    // File is in index but missing from working directory
                    var isInHeadCommit = headTreeFiles.TryGetValue(indexEntry.Path, out var headFileHash);
                    
                    if (isInHeadCommit && indexEntry.ObjectHash.Equals(headFileHash))
                    {
                        // File was committed and is now deleted from working directory
                        unstagedFile = new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            FileStatus.Deleted,
                            indexEntry.ObjectHash,
                            null);
                    }
                    else
                    {
                        // File is staged for addition/modification but missing from working directory
                        stagedFile = new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            isInHeadCommit ? FileStatus.Modified : FileStatus.Added,
                            isInHeadCommit ? (Hash?)headFileHash : null,
                            indexEntry.ObjectHash);
                        
                        unstagedFile = new PrimitiveWorkingDirectoryFile(
                            indexEntry.Path,
                            FileStatus.Deleted,
                            indexEntry.ObjectHash,
                            null);
                    }
                }

                return new Status(stagedFile, unstagedFile, processedPath);
            }));

        // Collect file lists
        var stagedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var unstagedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var untrackedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var processedPaths = new HashSet<string>();

        // Collect results
        foreach (var (stagedFile, unstagedFile, processedPath) in results)
        {
            if (stagedFile != null)
            {
                stagedFiles.Add(stagedFile.Value);
            }
            if (unstagedFile != null)
            {
                unstagedFiles.Add(unstagedFile.Value);
            }
            processedPaths.Add(processedPath);
        }

        // Find untracked files in working directory
        await ScanWorkingDirectoryAsync(
            repository, workingDirectoryPath, workingDirectoryPath, 
            processedPaths, untrackedFiles, pathFilter, ct);

        return new PrimitiveWorkingDirectoryStatus(
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(stagedFiles.ToArray()),
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(unstagedFiles.ToArray()),
            new ReadOnlyArray<PrimitiveWorkingDirectoryFile>(untrackedFiles.ToArray()));
    }

    /// <summary>
    /// Gets structured working directory status information for the specified repository.
    /// </summary>
    /// <param name="repository">The structured repository to get working directory status from.</param>
    /// <param name="rwr">The weak reference to the repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A ValueTask containing the structured working directory status.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static ValueTask<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct) =>
        GetStructuredWorkingDirectoryStatusWithFilterAsync(repository, rwr, Glob.includeAll, ct);
#else
    public static Task<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct) =>
        GetStructuredWorkingDirectoryStatusWithFilterAsync(repository, rwr, Glob.includeAll, ct);
#endif

    /// <summary>
    /// Gets structured working directory status information for the specified repository with file filtering.
    /// </summary>
    /// <param name="repository">The structured repository to get working directory status from.</param>
    /// <param name="rwr">The weak reference to the repository.</param>
    /// <param name="pathFilter">An optional predicate to filter files by path. If null, all files are included.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A ValueTask containing the structured working directory status.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusWithFilterAsync(
        StructuredRepository repository, WeakReference rwr, Func<string, bool> pathFilter, CancellationToken ct)
#else
    public static async Task<WorkingDirectoryStatus> GetStructuredWorkingDirectoryStatusWithFilterAsync(
        StructuredRepository repository, WeakReference rwr, Func<string, bool> pathFilter, CancellationToken ct)
#endif
    {
        var primitiveStatus = await GetPrimitiveWorkingDirectoryStatusWithFilterAsync(repository, pathFilter, ct);

        var stagedFiles = primitiveStatus.StagedFiles.
            Select(pf => new WorkingDirectoryFile(
                rwr, pf.Path, pf.Status, pf.IndexHash, pf.WorkingTreeHash)).
            ToArray();
        
        var unstagedFiles = primitiveStatus.UnstagedFiles.
            Select(pf => new WorkingDirectoryFile(
                rwr, pf.Path, pf.Status, pf.IndexHash, pf.WorkingTreeHash)).
            ToArray();
        
        var untrackedFiles = primitiveStatus.UntrackedFiles.
            Select(pf => new WorkingDirectoryFile(
                rwr, pf.Path, pf.Status, pf.IndexHash, pf.WorkingTreeHash)).
            ToArray();

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
        Func<string, bool> pathFilter,
        CancellationToken ct)
#else
    private static async Task ScanWorkingDirectoryAsync(
        Repository repository,
        string workingDirectoryPath,
        string currentPath,
        HashSet<string> processedPaths,
        List<PrimitiveWorkingDirectoryFile> untrackedFiles,
        Func<string, bool> pathFilter,
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
                        repository, workingDirectoryPath, entry, processedPaths, untrackedFiles, pathFilter, ct);
                }
                else if (await repository.fileSystem.IsFileExistsAsync(entry, ct))
                {
                    // Get relative path from working directory root
                    var relativePath = repository.fileSystem.GetRelativePath(workingDirectoryPath, entry);
                    
                    if (!processedPaths.Contains(relativePath))
                    {
                        // Apply path filter for untracked files
                        if (pathFilter(relativePath))
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