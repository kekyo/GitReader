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
    /// <returns>A combined predicate that returns true only if all predicates return true.</returns>
    /// <example>
    /// let filter1 = Glob.createIgnoreFilter([| "*.log" |])
    /// let filter2 = Glob.createIncludeFilter([| "*.cs"; "*.fs" |])
    /// let combined = Glob.combine([| filter1; filter2 |])
    /// </example>
    static member combine([<ParamArray>] predicates: (string -> bool)[]) =
        let funcPredicates = predicates |> Array.map (fun f -> Func<string, bool>(f))
        let combinedFunc = Internal.Glob.Combine(funcPredicates)
        fun path -> combinedFunc.Invoke(path)

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// let filter = Glob.createIgnoreFilter([| "*.log"; "bin/"; "obj/"; "node_modules/" |])
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    static member createIgnoreFilter([<ParamArray>] excludePatterns: string[]) =
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
    static member createIncludeFilter([<ParamArray>] includePatterns: string[]) =
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
    static member createCommonIgnoreFilter() =
        let delegateFilter = Internal.Glob.CreateCommonIgnoreFilter()
        fun path -> delegateFilter.Invoke(path)

    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async that returns a predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// use stream = File.OpenRead(".gitignore")
    /// let! filter = Glob.createGitignoreFilter(stream, ct)
    /// let shouldInclude = filter "somefile.txt"
    /// </example>
    static member createGitignoreFilter(gitignoreStream: Stream, ?ct: CancellationToken) =
        async {
            let! delegateFilter = Internal.Glob.CreateGitignoreFilterAsync(gitignoreStream, unwrapCT ct).asAsync()
            return fun path -> delegateFilter.Invoke(path)
        }

    /// <summary>
    /// Combines a .gitignore filter with an existing base filter.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async that returns a combined predicate function that applies both filters.</returns>
    /// <example>
    /// let baseFilter = Glob.createCommonIgnoreFilter()
    /// use stream = File.OpenRead(".gitignore")
    /// let! combinedFilter = Glob.combineWithGitignore(stream, Some baseFilter, ct)
    /// </example>
    static member combineWithGitignore(gitignoreStream: Stream, ?ct: CancellationToken) =
        async {
            let! delegateFilter = Internal.Glob.CombineWithGitignoreAsync(gitignoreStream, null, unwrapCT ct).asAsync()
            return fun path -> delegateFilter.Invoke(path)
        }

    /// <summary>
    /// Combines a .gitignore filter with an existing base filter.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="baseFilter">The base filter to combine with. If None, defaults to include all.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async that returns a combined predicate function that applies both filters.</returns>
    /// <example>
    /// let baseFilter = Glob.createCommonIgnoreFilter()
    /// use stream = File.OpenRead(".gitignore")
    /// let! combinedFilter = Glob.combineWithGitignore(stream, Some baseFilter, ct)
    /// </example>
    static member combineWithGitignore(gitignoreStream: Stream, baseFilter: (string -> bool), ?ct: CancellationToken) =
        async {
            let! delegateFilter = Internal.Glob.CombineWithGitignoreAsync(gitignoreStream, Func<string, bool>(baseFilter), unwrapCT ct).asAsync()
            return fun path -> delegateFilter.Invoke(path)
        }
