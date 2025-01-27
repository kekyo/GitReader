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

namespace GitReader.Structures;

internal interface IRepositoryReference
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    WeakReference Repository { get; }
}

public sealed class StructuredRepository : Repository
{
    internal Branch? head;
    internal ReadOnlyDictionary<string, Branch[]> branchesAll = null!;
    internal ReadOnlyDictionary<string, Tag> tags = null!;
    internal ReadOnlyArray<Stash> stashes = null!;

    private ReadOnlyDictionary<string, Branch>? branches;

    internal StructuredRepository(
        string repositoryPath,
        IFileSystem fileSystem) :
        base(repositoryPath, fileSystem)
    {
    }

    public Branch? Head =>
        this.head;

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

    public ReadOnlyDictionary<string, Tag> Tags =>
        this.tags;

    public ReadOnlyDictionary<string, Branch[]> BranchesAll =>
        this.branchesAll;

    public ReadOnlyArray<Stash> Stashes =>
        this.stashes;
}