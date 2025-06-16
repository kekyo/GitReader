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
/// Specifies the status of a file in the Git working directory.
/// </summary>
public enum FileStatus
{
    /// <summary>
    /// The file is unmodified.
    /// </summary>
    Unmodified = 0,
    
    /// <summary>
    /// The file has been added to the staging area.
    /// </summary>
    Added = 1,
    
    /// <summary>
    /// The file has been modified.
    /// </summary>
    Modified = 2,
    
    /// <summary>
    /// The file has been deleted.
    /// </summary>
    Deleted = 3,
    
    /// <summary>
    /// The file has been renamed.
    /// </summary>
    Renamed = 4,
    
    /// <summary>
    /// The file has been copied.
    /// </summary>
    Copied = 5,
    
    /// <summary>
    /// The file is untracked by Git.
    /// </summary>
    Untracked = 6,
    
    /// <summary>
    /// The file is ignored by Git.
    /// </summary>
    Ignored = 7,
}
