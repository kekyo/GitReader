////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader;

/// <summary>
/// Provides glob pattern matching functionality for .gitignore files.
/// </summary>
public static class Glob
{
    /// <summary>
    /// Determines whether the specified path matches the given glob pattern.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns>true if the path matches the pattern; otherwise, false.</returns>
    public static bool IsMatch(string path, string pattern) =>
        Internal.Glob.IsMatch(path, pattern);
    
    /// <summary>
    /// Combines multiple predicate functions using logical AND operation.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine.</param>
    /// <returns>A combined predicate that returns true only if all predicates return true.</returns>
    /// <example>
    /// var filter1 = Glob.CreateIgnoreFilter("*.log");
    /// var filter2 = Glob.CreateIncludeFilter("*.cs", "*.fs");
    /// var combined = Glob.Combine(filter1, filter2);
    /// </example>
    public static Func<string, bool> Combine(params Func<string, bool>[] predicates) =>
        Internal.Glob.Combine(predicates);
    
    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// var filter = Glob.CreateIgnoreFilter(new[] { "*.log", "bin/", "obj/", "node_modules/" });
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static Func<string, bool> CreateIgnoreFilter(params string[] excludePatterns) =>
        Internal.Glob.CreateIgnoreFilter(excludePatterns);

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes only files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="includePatterns">Array of .gitignore-style patterns to include files.</param>
    /// <returns>A predicate function that returns true if the path should be included.</returns>
    /// <example>
    /// var filter = Glob.CreateIncludeFilter(new[] { "*.cs", "*.fs", "*.ts" });
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static Func<string, bool> CreateIncludeFilter(params string[] includePatterns) =>
        Internal.Glob.CreateIncludeFilter(includePatterns);

    /// <summary>
    /// Get a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies standard .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// var filter = Glob.GetCommonIgnoreFilter();
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static Func<string, bool> GetCommonIgnoreFilter() =>
        Internal.Glob.GetCommonIgnoreFilter();

    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// using var stream = File.OpenRead(".gitignore");
    /// var filter = await Glob.CreateFilterFromGitignoreAsync(stream, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static ValueTask<Func<string, bool>> CreateFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default) =>
        Internal.Glob.CreateFilterFromGitignoreAsync(gitignoreStream, ct);
#else
    public static Task<Func<string, bool>> CreateFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default) =>
        Internal.Glob.CreateFilterFromGitignoreAsync(gitignoreStream, ct);
#endif
}
