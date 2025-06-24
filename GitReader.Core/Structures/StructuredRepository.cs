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
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GitReader.Internal;

namespace GitReader.Structures;

internal interface IRepositoryReference
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    WeakReference Repository { get; }
}

/// <summary>
/// Represents a structured Git repository that provides high-level access to Git objects and operations.
/// </summary>
public sealed class StructuredRepository : Repository
{
    internal Branch? head;
    internal ReadOnlyDictionary<string, Branch[]> branchesAll = null!;
    internal ReadOnlyDictionary<string, Tag> tags = null!;
    internal ReadOnlyArray<Stash> stashes = null!;

    private ReadOnlyDictionary<string, Branch>? branches;

    /// <summary>
    /// Initializes a new instance of the StructuredRepository class.
    /// </summary>
    /// <param name="gitPath">The path to the Git repository.</param>
    /// <param name="alternativePaths">Alternative paths to try when accessing the repository.</param>
    /// <param name="fileSystem">The file system implementation to use.</param>
    /// <param name="concurrentScope">Concurrent scope.</param>
    internal StructuredRepository(
        string gitPath,
        string[] alternativePaths,
        IFileSystem fileSystem,
        IConcurrentScope concurrentScope) :
        base(gitPath, alternativePaths, fileSystem, concurrentScope)
    {
    }

    /// <summary>
    /// Gets the current head branch of the repository, or null if the repository has no commits.
    /// </summary>
    public Branch? Head =>
        this.head;

    /// <summary>
    /// Gets a dictionary of all branches in the repository, keyed by branch name.
    /// When multiple branches have the same name (e.g., local and remote), only the first one is included.
    /// </summary>
    public ReadOnlyDictionary<string, Branch> Branches
    {
        get
        {
            if (this.branches == null)
            {
                // Ignored race condition.
                this.branches = this.branchesAll.ToDictionary(
                    entry => entry.Key, entry => entry.Value[0]);
            }
            return this.branches;
        }
    }

    /// <summary>
    /// Gets a dictionary of all tags in the repository, keyed by tag name.
    /// </summary>
    public ReadOnlyDictionary<string, Tag> Tags =>
        this.tags;

    /// <summary>
    /// Gets a dictionary of all branches in the repository, including multiple entries for the same branch name.
    /// This includes both local and remote branches that may have the same name.
    /// </summary>
    public ReadOnlyDictionary<string, Branch[]> BranchesAll =>
        this.branchesAll;

    /// <summary>
    /// Gets an array of all stashes in the repository.
    /// </summary>
    public ReadOnlyArray<Stash> Stashes =>
        this.stashes;
}