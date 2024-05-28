﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.IO;
using System;
using System.ComponentModel;

namespace GitReader.Structures;

internal interface IRepositoryReference
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    WeakReference Repository { get; }
}

public sealed class StructuredRepository : Repository
{
    internal Branch? head;
    internal ReadOnlyDictionary<string, Branch> branches = null!;
    internal ReadOnlyDictionary<string, Tag> tags = null!;
    internal ReadOnlyArray<Stash> stashes = null!;

    internal StructuredRepository(
        string repositoryPath,
        IFileSystem fileSystem) :
        base(repositoryPath, fileSystem)
    {
    }

    public Branch? Head =>
        this.head;

    public ReadOnlyDictionary<string, Branch> Branches =>
        this.branches;
    public ReadOnlyDictionary<string, Tag> Tags =>
        this.tags;    
    public ReadOnlyArray<Stash> Stashes =>
        this.stashes;
}
