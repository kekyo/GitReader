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
    /// Combines multiple predicate functions using hierarchical decision logic.
    /// Later predicates override earlier ones with non-Neutral decisions.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine.</param>
    /// <returns>A combined predicate that applies hierarchical filtering logic.</returns>
    /// <example>
    /// var filter1 = Glob.CreateExcludeFilter("*.log");
    /// var filter2 = Glob.CreateIncludeFilter("*.cs", "*.fs");
    /// var combined = Glob.Combine(filter1, filter2);
    /// </example>
    public static FilterDecisionDelegate Combine(params FilterDecisionDelegate[] predicates) =>
        Internal.Glob.Combine(predicates);
    
    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// var filter = Glob.CreateExcludeFilter(new[] { "*.log", "bin/", "obj/", "node_modules/" });
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static FilterDecisionDelegate CreateExcludeFilter(params string[] excludePatterns) =>
        Internal.Glob.CreateExcludeFilter(excludePatterns);

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes only files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="includePatterns">Array of .gitignore-style patterns to include files.</param>
    /// <returns>A predicate function that returns Include if the path should be included, or Neutral if undecided.</returns>
    /// <example>
    /// var filter = Glob.CreateIncludeFilter(new[] { "*.cs", "*.fs", "*.ts" });
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static FilterDecisionDelegate CreateIncludeFilter(params string[] includePatterns) =>
        Internal.Glob.CreateIncludeFilter(includePatterns);

    /// <summary>
    /// Get a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies embedded simple .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// var filter = Glob.GetCommonIgnoreFilter();
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static FilterDecisionDelegate GetCommonIgnoreFilter() =>
        Internal.Glob.commonIgnoreFilter;
    
    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that includes all files.
    /// </summary>
    /// <returns>Always returns Include.</returns>
    /// <example>
    /// var filter = Glob.GetIncludeAllFilter();
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static FilterDecisionDelegate GetIncludeAllFilter() =>
        Internal.Glob.includeAllFilter;

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes all files.
    /// </summary>
    /// <returns>Always returns Exclude.</returns>
    /// <example>
    /// var filter = Glob.GetExcludeAllFilter();
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static FilterDecisionDelegate GetExcludeAllFilter() =>
        Internal.Glob.excludeAllFilter;

    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// using var stream = File.OpenRead(".gitignore");
    /// var filter = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static ValueTask<FilterDecisionDelegate> CreateExcludeFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default) =>
        Internal.Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, ct);
#else
    public static Task<FilterDecisionDelegate> CreateExcludeFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default) =>
        Internal.Glob.CreateExcludeFilterFromGitignoreAsync(gitignoreStream, ct);
#endif
}
