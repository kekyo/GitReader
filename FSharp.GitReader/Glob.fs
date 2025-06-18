////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader

open System

/// <summary>
/// Provides glob pattern matching functionality for .gitignore files.
/// </summary>
/// 
[<AutoOpen>]
module public Glob =

    /// <summary>
    /// Determines whether the specified path matches the given glob pattern.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns>true if the path matches the pattern; otherwise, false.</returns>
    let public isMatch path pattern =
        Internal.Glob.IsMatch(path, pattern)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// let filter = Glob.createIgnoreFilter [| "*.log"; "bin/"; "obj/"; "node_modules/" |]
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    let public createIgnoreFilter ([<ParamArray>] excludePatterns) =
        let delegateFilter = Internal.Glob.CreateIgnoreFilter(excludePatterns)
        fun path -> delegateFilter.Invoke(path)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes only files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="includePatterns">Array of .gitignore-style patterns to include files.</param>
    /// <returns>A predicate function that returns true if the path should be included.</returns>
    /// <example>
    /// let filter = Glob.createIncludeFilter [| "*.cs"; "*.fs"; "*.ts" |]
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    let public createIncludeFilter ([<ParamArray>] includePatterns) =
        let delegateFilter = Internal.Glob.CreateIncludeFilter(includePatterns)
        fun path -> delegateFilter.Invoke(path)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies standard .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// let filter = Glob.createCommonIgnoreFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    let public createCommonIgnoreFilter() =
        let delegateFilter = Internal.Glob.CreateCommonIgnoreFilter()
        fun path -> delegateFilter.Invoke(path)
