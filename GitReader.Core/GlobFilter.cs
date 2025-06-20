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
/// Represents the evaluation state made by a glob filter for a specific path.
/// This enables proper hierarchical .gitignore processing where filters can explicitly
/// exclude, or remain neutral about specific paths.
/// </summary>
public enum GlobFilterStates
{
    /// <summary>
    /// This glob filter does not make an exclude state about this path (pattern does not match).
    /// The result should be deferred to other filters or default behavior.
    /// </summary>
    NotExclude,
    
    /// <summary>
    /// This glob filter explicitly excludes this path.
    /// This typically corresponds to normal patterns in .gitignore (e.g., pattern).
    /// </summary>
    Exclude
}

/// <summary>
/// Represents a delegate that determines the filter evaluation state for a specific path.
/// This allows for flexible filtering logic where the result can be influenced by the initial decision.
/// </summary>
public delegate GlobFilterStates GlobFilter(GlobFilterStates initialState, string path);
