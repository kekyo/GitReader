////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;

namespace GitReader.Structures;

/// <summary>
/// Represents a reference to a Git commit.
/// </summary>
public interface ICommitReference
{
    /// <summary>
    /// Gets the hash of the commit.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Hash Hash { get; }
}

internal interface IInternalCommitReference :
    ICommitReference, IRepositoryReference
{
}
