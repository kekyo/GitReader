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
open System.Runtime.CompilerServices
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
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member isMatch(path, pattern) =
        Internal.Glob.IsMatch(path, pattern)

    /// <summary>
    /// Combines multiple glob filter predicates into a single glob filter predicate that evaluates them in order.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine.</param>
    /// <returns>A combined predicate that applies hierarchical filtering logic.</returns>
    /// <example>
    /// let filter1 = Glob.createExcludeFilter([| "*.log"; "*.cs" |])
    /// let filter2 = Glob.createExcludeFilter([| "!important.log" |])
    /// let combined = Glob.combine([| filter1; filter2 |])
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member public combine([<ParamArray>] predicates) =
        Internal.Glob.Combine(predicates)

    /// <summary>
    /// Creates a glob path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, NotExclude if unevaluated.</returns>
    /// <example>
    /// let filter = Glob.createExcludeFilter([| "*.log"; "!important.log"; "bin/"; "obj/"; "node_modules/" |])
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member createExcludeFilter([<ParamArray>] excludePatterns) =
        Internal.Glob.CreateExcludeFilter(excludePatterns)

    /// <summary>
    /// Get a glob path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies embedded simple .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, NotExclude if unevaluated.</returns>
    /// <example>
    /// let filter = Glob.getCommonIgnoreFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member getCommonIgnoreFilter() =
        Internal.Glob.commonIgnoreFilter

    /// <summary>
    /// Creates a glob path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes all files.
    /// </summary>
    /// <returns>Always returns Exclude.</returns>
    /// <example>
    /// let filter = Glob.getExcludeAllFilter()
    /// let! status = repository.getWorkingDirectoryStatusWithFilter(filter)
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member getExcludeAllFilter() =
        Internal.Glob.excludeAllFilter

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <example>
    /// let stream = File.OpenRead(".gitignore")
    /// let! filter = Glob.createExcludeFilterFromGitignore(stream, ct)
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member createExcludeFilterFromGitignore(gitignoreStream: Stream, ?ct) =
        Internal.Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, unwrapCT ct).asAsync()

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreReader">Text reader containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <example>
    /// use reader = File.OpenText(".gitignore");
    /// let! filter = Glob.CreateExcludeFilterFromGitignoreAsync(reader, ct);
    /// </example>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member createExcludeFilterFromGitignore(gitignoreReader: TextReader, ?ct) =
        Internal.Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreReader, unwrapCT ct).asAsync()

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreLines">Text lines containing .gitignore content.</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <remarks>Difference from CreateExcludeFilter() is that empty lines and comments are evaluated.</remarks>
    [<MethodImpl(MethodImplOptions.NoInlining)>]
    static member createExcludeFilterFromGitignore(gitignoreLines: string seq) =
        Internal.Glob.CreateExcludeFilterFromGitignore(gitignoreLines)
