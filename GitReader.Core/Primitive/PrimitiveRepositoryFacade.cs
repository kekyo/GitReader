////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using GitReader.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

internal static class PrimitiveRepositoryFacade
{
    private static async Task<PrimitiveRepository> InternalOpenPrimitiveAsync(
        string repositoryPath,
        string[] alternativePaths,
        IFileSystem fileSystem,
        IConcurrentScope concurrentScope,
        CancellationToken ct)
    {
        var repository = new PrimitiveRepository(
            repositoryPath, alternativePaths, fileSystem, concurrentScope);

        try
        {
            // Must set remote urls first
            repository.remoteUrls = await RepositoryAccessor.ReadRemoteReferencesAsync(repository, ct);
            
            // Read FETCH_HEAD and packed-refs.
            var (fhc1, fhc2) = await repository.concurrentScope.Join(
                RepositoryAccessor.ReadFetchHeadsAsync(repository, ct),
                RepositoryAccessor.ReadPackedRefsAsync(repository, ct));
            repository.referenceCache = fhc1.Combine(fhc2);

            return repository;
        }
        catch
        {
            repository.Dispose();
            throw;
        }
    }

    public static async Task<PrimitiveRepository> OpenPrimitiveAsync(
        string path,
        IFileSystem fileSystem,
        IConcurrentScope concurrentScope,
        CancellationToken ct)
    {
        var (gitPath, alternativePaths) = await RepositoryAccessor.DetectLocalRepositoryPathAsync(
            path, fileSystem, ct);
        return await InternalOpenPrimitiveAsync(
            gitPath, alternativePaths, fileSystem, concurrentScope, ct);
    }

    //////////////////////////////////////////////////////////////////////////

    public static async Task<PrimitiveReference?> GetCurrentHeadReferenceAsync(
        Repository repository,
        CancellationToken ct)
    {
        var relativePathOrLocation = "HEAD";
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, relativePathOrLocation, ct);
        return results is { } r ?
            new PrimitiveReference(r.Names.Last(), relativePathOrLocation, r.Hash) :
            null;
    }

    public static async Task<PrimitiveReference> GetBranchHeadReferenceAsync(
        Repository repository,
        string branchName,
        CancellationToken ct)
    {
        var path = $"refs/heads/{branchName}";
        var remotePath = $"refs/remotes/{branchName}";
        var (results, remoteResults) = await repository.concurrentScope.Join(
            RepositoryAccessor.ReadHashAsync(repository, path, ct),
            RepositoryAccessor.ReadHashAsync(repository, remotePath, ct));
        return results is { } r ?
            new PrimitiveReference(branchName, path, r.Hash) :
            remoteResults is { } rr ?
            new PrimitiveReference(branchName, remotePath, rr.Hash) :
            throw new ArgumentException($"Could not find a branch: {branchName}");
    }

    public static async Task<PrimitiveReference[]> GetBranchAllHeadReferenceAsync(
        Repository repository,
        string branchName,
        CancellationToken ct)
    {
        var path = $"refs/heads/{branchName}";
        var remotePath = $"refs/remotes/{branchName}";
        var (results, remoteResults) = await repository.concurrentScope.Join(
            RepositoryAccessor.ReadHashAsync(repository, path, ct),
            RepositoryAccessor.ReadHashAsync(repository, remotePath, ct));
        return (results is { } r ?
            [ new PrimitiveReference(branchName, path, r.Hash) ] : Utilities.Empty<PrimitiveReference>()).
            Concat(remoteResults is { } rr ?
                [ new PrimitiveReference(branchName, remotePath, rr.Hash) ] : Utilities.Empty<PrimitiveReference>()).
            ToArray();
    }

    public static async Task<PrimitiveTagReference> GetTagReferenceAsync(
        Repository repository,
        string tagName, CancellationToken ct)
    {
        var relativePathOrLocation = $"refs/tags/{tagName}";
        var results = await RepositoryAccessor.ReadHashAsync(
            repository, relativePathOrLocation, ct);
        return results is { } r ?
            new PrimitiveTagReference(tagName, relativePathOrLocation, r.Hash, null) :
            throw new ArgumentException($"Could not find a tag: {tagName}");
    }

    /// <summary>
    /// Gets a primitive tag from the specified tag reference with proper peeled-tag handling.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="tagReference">The tag reference.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns the primitive tag.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<PrimitiveTag> GetTagAsync(
        Repository repository,
        PrimitiveTagReference tagReference,
        CancellationToken ct)
#else
    public static async Task<PrimitiveTag> GetTagAsync(
        Repository repository,
        PrimitiveTagReference tagReference,
        CancellationToken ct)
#endif
    {
        // If produced a peeled-tag, we can get the commit hash with no additional costs.
        if (tagReference.CommitHash is { } commitTarget)
        {
            return new PrimitiveTag(
                commitTarget,
                ObjectTypes.Commit,
                tagReference.Name,
                null,
                null);
        }
        // If peeled-tags are not provided by the 'packed-refs' file at open time,
        // a tag object will be read occur here. This is expensive and extends open time.
        // However, since the commit hash cannot be identified without reading the tag object
        // (given that this is a high-level interface), a compromise is made.
        else if (await RepositoryAccessor.ReadTagAsync(
            repository, tagReference.ObjectOrCommitHash, ct) is { } tag)
        {
            return new PrimitiveTag(
                tag.Hash,
                tag.Type,
                tagReference.Name,
                tag.Tagger,
                tag.Message);
        }
        // If the read result shows that it is not a tag object, it is a commit object.
        else
        {
            return new PrimitiveTag(
                tagReference.ObjectOrCommitHash,
                ObjectTypes.Commit,
                tagReference.Name,
                null,
                null);
        }
    }

    /// <summary>
    /// Gets all branch head references that point to the specified commit.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="commitHash">The commit hash to find related branches for.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of related branch head references.</returns>
    public static async Task<PrimitiveReference[]> GetRelatedBranchHeadReferencesAsync(
        Repository repository,
        Hash commitHash,
        CancellationToken ct)
    {
        // Get both local and remote branches in parallel
        var (localBranches, remoteBranches) = await repository.concurrentScope.Join(
            RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.Branches, ct),
            RepositoryAccessor.ReadReferencesAsync(repository, ReferenceTypes.RemoteBranches, ct));

        // Extract branch references that point to the specified commit hash
        return localBranches.
            Where(branch => branch.Target.Equals(commitHash)).
            Concat(remoteBranches.Where(branch => branch.Target.Equals(commitHash))).
            ToArray();
    }

    /// <summary>
    /// Gets all tag references that point to the specified commit.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="commitHash">The commit hash to find related tags for.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of related tag references.</returns>
    public static async Task<PrimitiveTagReference[]> GetRelatedTagReferencesAsync(
        Repository repository,
        Hash commitHash,
        CancellationToken ct)
    {
        // Get all tag references
        var allTagReferences = await RepositoryAccessor.ReadTagReferencesAsync(repository, ct);
        
        // Filter tags that point to the specified commit
        var relatedTags = await repository.concurrentScope.WhenAll(ct,
            allTagReferences.Select(async tagReference =>
            {
                // If we have peeled-tag information available, use it for efficient comparison
                if (tagReference.CommitHash is { } commitTarget)
                {
                    return commitTarget.Equals(commitHash) ?
                        tagReference : (PrimitiveTagReference?)null;
                }

                // For lightweight tags or when peeled-tag info is not available,
                // we need to check if ObjectOrCommitHash directly points to our commit
                if (tagReference.ObjectOrCommitHash.Equals(commitHash))
                {
                    return tagReference;
                }

                // For annotated tags without peeled-tag info, we need to read the tag object
                // to determine the actual commit it points to
                var primitiveTag = await GetTagAsync(repository, tagReference, ct);
                
                return primitiveTag.Hash.Equals(commitHash) ?
                    tagReference : (PrimitiveTagReference?)null;
            }));

        return relatedTags.
            Where(tag => tag.HasValue).
            Select(tag => tag!.Value).
            ToArray();
     }

     /// <summary>
     /// Gets all tags that point to the specified commit.
     /// </summary>
     /// <param name="repository">The repository.</param>
     /// <param name="commitHash">The commit hash to find related tags for.</param>
     /// <param name="ct">The cancellation token.</param>
     /// <returns>A task that returns an array of related tags.</returns>
     public static async Task<PrimitiveTag[]> GetRelatedTagsAsync(
         Repository repository,
         Hash commitHash,
         CancellationToken ct)
     {
         // Get all tag references
         var allTagReferences = await RepositoryAccessor.ReadTagReferencesAsync(repository, ct);
         
         // Filter tags that point to the specified commit
         var relatedTags = await repository.concurrentScope.WhenAll(ct,
             allTagReferences.Select(async tagReference =>
             {
                 // If we have peeled-tag information available, use it for efficient comparison
                 if (tagReference.CommitHash is { } commitTarget)
                 {
                     if (commitTarget.Equals(commitHash))
                     {
                         // Need to get the actual tag object
                         return await GetTagAsync(repository, tagReference, ct);
                     }
                     return (PrimitiveTag?)null;
                 }
                 
                 // For lightweight tags or when peeled-tag info is not available,
                 // we need to check if ObjectOrCommitHash directly points to our commit
                 if (tagReference.ObjectOrCommitHash.Equals(commitHash))
                 {
                     return await GetTagAsync(repository, tagReference, ct);
                 }
                 
                 // For annotated tags without peeled-tag info, we need to read the tag object
                 // to determine the actual commit it points to
                 var primitiveTag = await GetTagAsync(repository, tagReference, ct);
                 
                 return primitiveTag.Hash.Equals(commitHash) ?
                     primitiveTag : (PrimitiveTag?)null;
             }));
         
         return relatedTags.
             Where(tag => tag.HasValue).
             Select(tag => tag!.Value).
             ToArray();
     }

     public static async Task<PrimitiveRepository> OpenSubModuleAsync(
        Repository repository,
        PrimitiveTreeEntry[] treePath, CancellationToken ct)
    {
        if (treePath.Length == 0)
        {
            throw new ArgumentException("Could not empty tree path.");
        }
        if (treePath[treePath.Length - 1].SpecialModes != PrimitiveSpecialModes.SubModule)
        {
            throw new ArgumentException($"Could not use non-submodule entry: {treePath[treePath.Length - 1]}");
        }
        
        if (await RepositoryAccessor.GetCandidateFilePathAsync(
            repository, repository.fileSystem.Combine(
                "modules",
                repository.fileSystem.Combine(treePath.Select(tree => tree.Name).ToArray()),
                "config"), ct) is not { } cp)
        {
            throw new ArgumentException("Submodule repository does not exist.");
        }

        return await InternalOpenPrimitiveAsync(
            cp.BasePath, [], repository.fileSystem, repository.concurrentScope, ct);
    }

    //////////////////////////////////////////////////////////////////////////

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
    /// Gets primitive working directory status information for the specified repository.
    /// </summary>
    /// <param name="repository">The repository to get working directory status from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A ValueTask containing the primitive working directory status.</returns>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<PrimitiveWorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct)
#else
    public static async Task<PrimitiveWorkingDirectoryStatus> GetWorkingDirectoryStatusAsync(
        Repository repository, CancellationToken ct)
#endif
    {
        async Task<Dictionary<string, GitIndexEntry>> GetStagedFilesAsync()
        {
            // Get staged files from index
            var indexEntries = await GitIndexReader.ReadIndexEntriesAsync(repository, ct);
            var dict = indexEntries.
                Where(entry => entry is { IsValidFlag: true, IsStageFlag: false }).
                ToDictionary(entry => entry.Path);
            return dict;
        }

        async Task<Dictionary<string, Hash>> GetCommitHeadTreeFilesAsync()
        {
            // Get the current commit's tree for comparison
            var headReference = await GetCurrentHeadReferenceAsync(repository, ct);
            var headTreeFiles = new Dictionary<string, Hash>();
            if (headReference != null)
            {
                var headCommit = await RepositoryAccessor.ReadCommitAsync(repository, headReference.Value.Target, ct);
                if (headCommit != null)
                {
                    // Build dictionary of files from HEAD commit tree
                    await WorkingDirectoryAccessor.BuildTreeFileDictionaryAsync(
                        repository, headCommit.Value.TreeRoot, "", headTreeFiles, ct);
                }
            }
            return headTreeFiles;
        }

        var (indexFileDict, headTreeFiles) = await repository.concurrentScope.
            Join(GetStagedFilesAsync(), GetCommitHeadTreeFilesAsync());

        // Get working directory path
        var workingDirectoryPath = repository.fileSystem.GetDirectoryPath(repository.GitPath);

        // Process index entries in parallel
        var results = await repository.concurrentScope.WhenAll(ct,
            indexFileDict.Values.Select((async indexEntry =>
            {
                var stagedFile = (PrimitiveWorkingDirectoryFile?)null;
                var unstagedFile = (PrimitiveWorkingDirectoryFile?)null;
                var processedPath = indexEntry.Path;
                var workingFilePath = repository.fileSystem.Combine(workingDirectoryPath, indexEntry.Path);
                
                if (await repository.fileSystem.IsFileExistsAsync(workingFilePath, ct))
                {
                    // File exists in working directory
                    var workingHash = await WorkingDirectoryAccessor.CalculateFileHashAsync(
                        repository, workingFilePath, ct);
                    
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
            })));

        // Collect file lists
        var stagedFiles = new List<PrimitiveWorkingDirectoryFile>();
        var unstagedFiles = new List<PrimitiveWorkingDirectoryFile>();
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

        return new PrimitiveWorkingDirectoryStatus(
            workingDirectoryPath,
            new(stagedFiles.OrderBy(f => f.Path).ToArray()),      // Make stable.
            new(unstagedFiles.OrderBy(f => f.Path).ToArray()),
            new(processedPaths.OrderBy(p => p).ToArray()));
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<ReadOnlyArray<PrimitiveWorkingDirectoryFile>> GetUntrackedFilesAsync(
        Repository repository,
        PrimitiveWorkingDirectoryStatus workingDirectoryStatus,
        GlobFilter overrideGlobFilter,
        CancellationToken ct)
#else
    public static async Task<ReadOnlyArray<PrimitiveWorkingDirectoryFile>> GetUntrackedFilesAsync(
        Repository repository,
        PrimitiveWorkingDirectoryStatus workingDirectoryStatus,
        GlobFilter overrideGlobFilter,
        CancellationToken ct)
#endif
    {
        var untrackedFiles = new List<PrimitiveWorkingDirectoryFile>();

        // Find untracked files in working directory
        await WorkingDirectoryAccessor.ExtractUntrackedFilesAsync(
            repository,
            workingDirectoryStatus.workingDirectoryPath,
            new(workingDirectoryStatus.processedPaths),
            overrideGlobFilter,    // Override path filter
            untrackedFiles,        // Results
            ct);

        // Make stable.
        return new(untrackedFiles.OrderBy(f => f.Path).ToArray());
    }
}
