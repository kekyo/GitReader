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
/// Represents the decision made by a filter for a specific path.
/// This enables proper hierarchical .gitignore processing where filters can explicitly
/// include, exclude, or remain neutral about specific paths.
/// </summary>
public enum FilterDecision
{
    /// <summary>
    /// This filter does not make a decision about this path (pattern does not match).
    /// The decision should be deferred to other filters or default behavior.
    /// </summary>
    Neutral,
    
    /// <summary>
    /// This filter explicitly includes this path.
    /// This typically corresponds to negation patterns in .gitignore (e.g., !pattern).
    /// </summary>
    Include,
    
    /// <summary>
    /// This filter explicitly excludes this path.
    /// This typically corresponds to normal patterns in .gitignore (e.g., pattern).
    /// </summary>
    Exclude
}

/// <summary>
/// Represents a delegate that determines the filter decision for a specific path.
/// This allows for flexible filtering logic where the decision can be influenced by the initial decision.
/// </summary>
public delegate FilterDecision FilterDecisionDelegate(string path, FilterDecision initialDecision);
