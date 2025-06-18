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
open System.IO
open System.Threading

/// <summary>
/// Provides glob pattern matching functionality for .gitignore files.
/// </summary>
/// 
[<AbstractClass; Sealed>]
type public Glob =

    /// <summary>
    /// Determines whether the specified path matches the given glob pattern.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns>true if the path matches the pattern; otherwise, false.</returns>
    static member isMatch(path: string, pattern: string) =
        Internal.Glob.IsMatch(path, pattern)

    /// <summary>
    /// Combines multiple predicate functions using logical AND operation.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine.</param>
    /// <returns>A combined predicate that returns Include if any predicate returns Include, or Exclude if all predicates return Exclude.</returns>
    /// <example>
    /// let filter1 = Glob.createExcludeFilter([| "*.log" |])
    /// let filter2 = Glob.createIncludeFilter([| "*.cs"; "*.fs" |])
    /// let combined = Glob.combine([| filter1; filter2 |])
    /// </example>
    static member combine([<ParamArray>] predicates: FilterDecisionDelegate[]) =
        Internal.Glob.Combine(predicates)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// let filter = Glob.createExcludeFilter([| "*.log"; "bin/"; "obj/"; "node_modules/" |])
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member createExcludeFilter([<ParamArray>] excludePatterns: string[]) =
        Internal.Glob.CreateExcludeFilter(excludePatterns)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes only files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="includePatterns">Array of .gitignore-style patterns to include files.</param>
    /// <returns>A predicate function that returns Include if the path should be included, or Neutral if undecided.</returns>
    /// <example>
    /// let filter = Glob.createIncludeFilter [| "*.cs"; "*.fs"; "*.ts" |]
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member createIncludeFilter([<ParamArray>] includePatterns: string[]) =
        Internal.Glob.CreateIncludeFilter(includePatterns)

    /// <summary>
    /// Get a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies embedded simple .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// let filter = Glob.getCommonIgnoreFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member getCommonIgnoreFilter() =
        Internal.Glob.commonIgnoreFilter

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes all files.
    /// </summary>
    /// <returns>Always returns Include.</returns>
    /// <example>
    /// let filter = Glob.getIncludeAllFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member getIncludeAllFilter() =
        Internal.Glob.includeAllFilter

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes all files.
    /// </summary>
    /// <returns>Always returns Exclude.</returns>
    /// <example>
    /// let filter = Glob.getExcludeAllFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member getExcludeAllFilter() =
        Internal.Glob.excludeAllFilter
    
    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// let stream = File.OpenRead(".gitignore")
    /// let! filter = Glob.createExcludeFilterFromGitignore(stream, ct)
    /// </example>
    static member createExcludeFilterFromGitignore(gitignoreStream: Stream, ?ct: CancellationToken) =
        Internal.Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, unwrapCT ct).asAsync()
