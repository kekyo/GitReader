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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class WorktreeAccessor
{
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<ReadOnlyArray<PrimitiveWorktree>> GetPrimitiveWorktreesAsync(
        Repository repository, CancellationToken ct)
#else
    public static async Task<ReadOnlyArray<PrimitiveWorktree>> GetPrimitiveWorktreesAsync(
        Repository repository, CancellationToken ct)
#endif
    {
        var worktrees = new List<PrimitiveWorktree>();

        // Add main worktree
        var mainWorktreePath = repository.fileSystem.GetDirectoryPath(repository.GitPath);
        
        worktrees.Add(new PrimitiveWorktree(
            repository,
            "(main)",
            mainWorktreePath,
            WorktreeStatus.Normal));

        // Get additional worktrees from worktrees directory
        var worktreesPath = repository.fileSystem.Combine(repository.GitPath, "worktrees");
        if (await repository.fileSystem.IsDirectoryExistsAsync(worktreesPath, ct))
        {
            var worktreeDirectories = await repository.fileSystem.GetDirectoryEntriesAsync(worktreesPath, ct);
            
            worktrees.AddRange((await Utilities.WhenAll(
                worktreeDirectories.Select(async worktreeDir =>
                {
                    if (await repository.fileSystem.IsDirectoryExistsAsync(worktreeDir, ct))
                    {
                        var worktreeName = repository.fileSystem.GetFileName(worktreeDir);
                        return await GetWorktreeInfoAsync(repository, worktreeDir, worktreeName, ct);
                    }
                    else
                    {
                        return null;
                    }
                }))).
                CollectValue(worktree => worktree));
        }

        return worktrees.ToArray();
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<ReadOnlyArray<Worktree>> GetStructuredWorktreesAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct)
#else
    public static async Task<ReadOnlyArray<Worktree>> GetStructuredWorktreesAsync(
        StructuredRepository repository, WeakReference rwr, CancellationToken ct)
#endif
    {
        var primitiveWorktrees = await GetPrimitiveWorktreesAsync(repository, ct);
        return await Utilities.WhenAll(primitiveWorktrees.Select(async worktree =>
            {
                var head = await GetWorktreeHeadAsync(worktree, ct);
                var branch = await GetWorktreeBranchAsync(worktree, ct);
                return new Worktree(rwr, worktree.Name, worktree.Path, head, branch, worktree.Status);
            }));
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    internal static async ValueTask<Hash?> GetWorktreeHeadAsync(
        PrimitiveWorktree worktree, CancellationToken ct)
#else
    internal static async Task<Hash?> GetWorktreeHeadAsync(
        PrimitiveWorktree worktree, CancellationToken ct)
#endif
    {
        try
        {
            if (worktree.IsMain)
            {
                var results = await RepositoryAccessor.ReadHashAsync(worktree.Repository, "HEAD", ct);
                return results?.Hash;
            }
            if (worktree.WorktreeDir != null)
            {
                var headPath = worktree.Repository.fileSystem.Combine(worktree.WorktreeDir, "HEAD");
                if (await worktree.Repository.fileSystem.IsFileExistsAsync(headPath, ct))
                {
                    using var headFs = await worktree.Repository.fileSystem.OpenAsync(headPath, false, ct);
                    var headTr = new AsyncTextReader(headFs);
                    var headContent = (await headTr.ReadToEndAsync(ct)).Trim();

                    if (headContent.StartsWith("ref: "))
                    {
                        // Branch reference
                        var refPath = headContent.Substring(5);
                        var results = await RepositoryAccessor.ReadHashAsync(worktree.Repository, refPath, ct);
                        return results?.Hash;
                    }
                    else
                    {
                        // Direct hash (detached HEAD)
                        if (Hash.TryParse(headContent, out var parsedHash))
                        {
                            return parsedHash;
                        }
                    }
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    internal static async ValueTask<string?> GetWorktreeBranchAsync(
        PrimitiveWorktree worktree, CancellationToken ct)
#else
    internal static async Task<string?> GetWorktreeBranchAsync(
        PrimitiveWorktree worktree, CancellationToken ct)
#endif
    {
        try
        {
            if (worktree.IsMain)
            {
                var headPath = worktree.Repository.fileSystem.Combine(worktree.Repository.GitPath, "HEAD");
                if (!await worktree.Repository.fileSystem.IsFileExistsAsync(headPath, ct))
                {
                    return null;
                }

                using var fs = await worktree.Repository.fileSystem.OpenAsync(headPath, false, ct);
                var tr = new AsyncTextReader(fs);
                var headContent = (await tr.ReadToEndAsync(ct)).Trim();

                if (headContent.StartsWith("ref: refs/heads/"))
                {
                    return headContent.Substring(16);
                }

                return null;
            }
            if (worktree.WorktreeDir != null)
            {
                var headPath = worktree.Repository.fileSystem.Combine(worktree.WorktreeDir, "HEAD");
                if (await worktree.Repository.fileSystem.IsFileExistsAsync(headPath, ct))
                {
                    using var headFs = await worktree.Repository.fileSystem.OpenAsync(headPath, false, ct);
                    var headTr = new AsyncTextReader(headFs);
                    var headContent = (await headTr.ReadToEndAsync(ct)).Trim();

                    if (headContent.StartsWith("ref: "))
                    {
                        // Branch reference
                        var refPath = headContent.Substring(5);
                        if (refPath.StartsWith("refs/heads/"))
                        {
                            return refPath.Substring(11);
                        }
                    }
                }
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    private static async ValueTask<PrimitiveWorktree?> GetWorktreeInfoAsync(
        Repository repository, string worktreeDir, string worktreeName, CancellationToken ct)
#else
    private static async Task<PrimitiveWorktree?> GetWorktreeInfoAsync(
        Repository repository, string worktreeDir, string worktreeName, CancellationToken ct)
#endif
    {
        try
        {
            // Read gitdir file to get worktree path
            var gitdirPath = repository.fileSystem.Combine(worktreeDir, "gitdir");
            if (!await repository.fileSystem.IsFileExistsAsync(gitdirPath, ct))
            {
                return null;
            }

            using var fs = await repository.fileSystem.OpenAsync(gitdirPath, false, ct);
            var tr = new AsyncTextReader(fs);
            var worktreePath = (await tr.ReadToEndAsync(ct)).Trim();
            
            if (string.IsNullOrEmpty(worktreePath))
            {
                return null;
            }

            // If gitdir points to a .git file, get the parent directory
            if (repository.fileSystem.GetFileName(worktreePath) == ".git")
            {
                worktreePath = repository.fileSystem.GetDirectoryPath(worktreePath);
            }

            // Check if worktree path exists
            if (!await repository.fileSystem.IsDirectoryExistsAsync(worktreePath, ct))
            {
                return new PrimitiveWorktree(
                    repository,
                    worktreeName,
                    worktreePath,
                    WorktreeStatus.Prunable,
                    worktreeDir);
            }

            // Check if worktree is locked
            var lockedPath = repository.fileSystem.Combine(worktreeDir, "locked");
            var isLocked = await repository.fileSystem.IsFileExistsAsync(lockedPath, ct);

            // Determine status
            var status = WorktreeStatus.Normal;
            var headPath = repository.fileSystem.Combine(worktreeDir, "HEAD");
            
            if (isLocked)
            {
                status = WorktreeStatus.Locked;
            }
            else if (await repository.fileSystem.IsFileExistsAsync(headPath, ct))
            {
                using var headFs = await repository.fileSystem.OpenAsync(headPath, false, ct);
                var headTr = new AsyncTextReader(headFs);
                var headContent = (await headTr.ReadToEndAsync(ct)).Trim();

                if (!headContent.StartsWith("ref: ") && Hash.TryParse(headContent, out _))
                {
                    // Direct hash (detached HEAD)
                    status = WorktreeStatus.Detached;
                }
            }

            return new PrimitiveWorktree(
                repository,
                worktreeName,
                worktreePath,
                status,
                worktreeDir);
        }
        catch
        {
            // If any error occurs, return null
            return null;
        }
    }

    // Legacy methods for compatibility (removed since they're no longer needed)
} 