////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader;

/// <summary>
/// Specifies the types of Git objects.
/// </summary>
public enum ObjectTypes
{
    /// <summary>
    /// Represents a Git commit object.
    /// </summary>
    Commit = 1,
    
    /// <summary>
    /// Represents a Git tree object.
    /// </summary>
    Tree,
    
    /// <summary>
    /// Represents a Git blob object.
    /// </summary>
    Blob,
    
    /// <summary>
    /// Represents a Git tag object.
    /// </summary>
    Tag,
}
