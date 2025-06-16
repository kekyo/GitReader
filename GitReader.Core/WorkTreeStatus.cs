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
/// Specifies the status of a Git worktree.
/// </summary>
public enum WorktreeStatus
{
    /// <summary>
    /// The worktree is in normal state.
    /// </summary>
    Normal,
    
    /// <summary>
    /// The worktree is a bare repository.
    /// </summary>
    Bare,
    
    /// <summary>
    /// The worktree is in a detached HEAD state.
    /// </summary>
    Detached,
    
    /// <summary>
    /// The worktree is locked.
    /// </summary>
    Locked,
    
    /// <summary>
    /// The worktree is prunable (can be removed).
    /// </summary>
    Prunable
}
