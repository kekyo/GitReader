////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitReader.IO;

namespace GitReader.Internal;

/// <summary>
/// Provides glob pattern matching functionality for .gitignore files.
/// </summary>
internal static class Glob
{
    /// <summary>
    /// Specifies the behavior mode for pattern-based filtering.
    /// </summary>
    private enum PatternFilterMode
    {
        /// <summary>
        /// Ignore mode: Files matching patterns are excluded, non-matching files are included.
        /// This is the standard .gitignore behavior where patterns specify what to ignore.
        /// - Empty patterns: Include all files
        /// - No pattern matches: Include the file (default)
        /// - Normal pattern matches: Exclude the file
        /// - Negation pattern (!) matches: Include the file (override previous exclusion)
        /// </summary>
        Ignore,

        /// <summary>
        /// Include mode: Only files matching patterns are included, non-matching files are excluded.
        /// This is useful for allowlist-style filtering where patterns specify what to include.
        /// - Empty patterns: Exclude all files
        /// - No pattern matches: Exclude the file (default)
        /// - Normal pattern matches: Include the file
        /// - Negation pattern (!) matches: Exclude the file (override previous inclusion)
        /// </summary>
        Include
    }

    private static readonly string[] commonPatterns =
    [
        // Build outputs
        "bin/", "obj/", "build/", "out/", "target/", "dist/",
        // Dependencies
        "node_modules/", "packages/", "vendor/",
        // Log files
        "*.log", "logs/",
        // Temporary files
        "*.tmp", "*.temp", "*.swp", "*.bak", "*~",
        // IDE files
        ".vs/", ".vscode/", ".idea/", "*.suo", "*.user",
        // OS files
        ".DS_Store", "Thumbs.db", "Desktop.ini"
    ];

    internal static readonly Func<string, bool> includeAllFilter = _ => true;
    internal static readonly Func<string, bool> ignoreAllFilter = _ => false;
    
    private static readonly Func<string, bool> commonIgnoreFilter =
        CreateCommonPatternFilter(commonPatterns, PatternFilterMode.Ignore);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Determines whether the specified path matches the given glob pattern.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns>true if the path matches the pattern; otherwise, false.</returns>
    public static bool IsMatch(string path, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        // Handle negation pattern
        var isNegated = false;
        if (pattern.StartsWith("!"))
        {
            isNegated = true;
            pattern = pattern.Substring(1);
        }

        // Handle comments and empty lines
        if (pattern.StartsWith("#") || pattern.Length == 0)
            return false;

        // Normalize path separators to forward slashes (only for path, not pattern)
        path = path.Replace('\\', '/');
        
        // Remove trailing slashes from path
        path = path.TrimEnd('/');

        // Normalize consecutive slashes in pattern to single slash
        while (pattern.Contains("//"))
        {
            pattern = pattern.Replace("//", "/");
        }

        var result = MatchesPattern(path, pattern);
        
        return isNegated ? !result : result;
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Handle directory-only patterns (ending with /)
        bool directoryOnly = pattern.EndsWith("/");
        if (directoryOnly)
        {
            pattern = pattern.TrimEnd('/');
        }

        // Handle absolute patterns (starting with /)
        bool isAbsolute = pattern.StartsWith("/");
        if (isAbsolute)
        {
            pattern = pattern.Substring(1);
            bool result = MatchGlob(path, pattern);
            
            // For directory patterns, also check if path starts with the directory
            if (directoryOnly && !result)
            {
                result = path.StartsWith(pattern + "/");
            }
            
            return result || (directoryOnly && path.Length == 0); // Root directory case
        }

        // For relative patterns, check if pattern matches at any level
        if (pattern.Contains("/"))
        {
            // Pattern contains path separator - match from current level
            bool result = MatchGlob(path, pattern);
            
            // For directory patterns, also check if path starts with the directory
            if (directoryOnly && !result)
            {
                result = path.StartsWith(pattern + "/");
            }
            
            return result;
        }
        else
        {
            // Pattern is just a filename - match against filename only
            return MatchFileNameAtAnyLevel(path, pattern);
        }
    }

    private static bool MatchFileNameAtAnyLevel(string path, string pattern)
    {
        // For empty path, check if pattern matches empty string directly
        if (path.Length == 0)
            return MatchGlob("", pattern);

        // Pattern without slashes should match at any directory level
        // Check if the full path matches (for files in root)
        if (MatchGlob(path, pattern))
            return true;

        // Check all path components  
        var parts = path.Split('/');
        foreach (var part in parts)
        {
            if (MatchGlob(part, pattern))
                return true;
        }

        return false;
    }

    private static bool MatchGlob(string text, string pattern)
    {
        return MatchGlobRecursive(text, 0, pattern, 0);
    }

    private static bool MatchGlobRecursive(string text, int textIndex, string pattern, int patternIndex)
    {
        // Special case: if pattern is just *, it matches any string (including empty)
        if (patternIndex < pattern.Length && pattern[patternIndex] == '*' && 
            patternIndex + 1 == pattern.Length)
        {
            // Single * at end of pattern matches anything (including empty string)
            // Check if remaining text has no path separators for single *
            for (int i = textIndex; i < text.Length; i++)
            {
                if (text[i] == '/')
                    return false;
            }
            return true;
        }

        while (patternIndex < pattern.Length)
        {
            char patternChar = pattern[patternIndex];

            if (patternChar == '*')
            {
                // Handle ** (directory wildcard)
                if (patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*')
                {
                    return MatchDoubleAsterisk(text, textIndex, pattern, patternIndex + 2);
                }

                // Handle single * (matches anything except /)
                return MatchSingleAsterisk(text, textIndex, pattern, patternIndex + 1);
            }
            else if (patternChar == '?')
            {
                // ? matches any single character except /
                if (textIndex >= text.Length || text[textIndex] == '/')
                    return false;
                
                textIndex++;
                patternIndex++;
            }
            else if (patternChar == '[')
            {
                // Character class [abc] or [!abc] or [a-z]
                int nextPatternIndex;
                bool matched = MatchCharacterClass(text, textIndex, pattern, patternIndex, out nextPatternIndex);
                if (!matched)
                    return false;
                
                textIndex++;
                patternIndex = nextPatternIndex;
            }
            else if (patternChar == '\\')
            {
                // Escape character
                if (patternIndex + 1 >= pattern.Length)
                    return false;
                
                patternIndex++; // Skip the backslash
                if (textIndex >= text.Length || text[textIndex] != pattern[patternIndex])
                    return false;
                
                textIndex++;
                patternIndex++;
            }
            else
            {
                // Literal character
                if (textIndex >= text.Length || text[textIndex] != patternChar)
                    return false;
                
                textIndex++;
                patternIndex++;
            }
        }

        // Pattern consumed - check if text is also consumed
        return textIndex == text.Length;
    }

    private static bool MatchSingleAsterisk(string text, int textIndex, string pattern, int patternIndex)
    {
        // * matches zero or more characters except /
        
        // Try matching zero characters
        if (MatchGlobRecursive(text, textIndex, pattern, patternIndex))
            return true;

        // Try matching one or more characters
        for (int i = textIndex; i < text.Length && text[i] != '/'; i++)
        {
            if (MatchGlobRecursive(text, i + 1, pattern, patternIndex))
                return true;
        }

        return false;
    }

    private static bool MatchDoubleAsterisk(string text, int textIndex, string pattern, int patternIndex)
    {
        // ** matches zero or more path segments
        
        // Skip any following / in pattern
        while (patternIndex < pattern.Length && pattern[patternIndex] == '/')
            patternIndex++;

        // If no more pattern, ** matches rest of the path
        if (patternIndex >= pattern.Length)
            return true;

        // Try matching at current position and all subsequent positions
        for (int i = textIndex; i <= text.Length; i++)
        {
            if (MatchGlobRecursive(text, i, pattern, patternIndex))
                return true;

            // Move to next path segment
            if (i < text.Length)
            {
                while (i < text.Length && text[i] != '/')
                    i++;
            }
        }

        return false;
    }

    private static bool MatchCharacterClass(
        string text, int textIndex, string pattern, int patternIndex, out int nextPatternIndex)
    {
        nextPatternIndex = patternIndex;
        
        if (textIndex >= text.Length || text[textIndex] == '/')
            return false;

        char textChar = text[textIndex];
        patternIndex++; // Skip opening [

        bool negated = false;
        if (patternIndex < pattern.Length && pattern[patternIndex] == '!')
        {
            negated = true;
            patternIndex++;
        }

        bool matched = false;
        while (patternIndex < pattern.Length && pattern[patternIndex] != ']')
        {
            char patternChar = pattern[patternIndex];

            // Handle character ranges like a-z
            if (patternIndex + 2 < pattern.Length && 
                pattern[patternIndex + 1] == '-' && 
                pattern[patternIndex + 2] != ']')
            {
                char startChar = patternChar;
                char endChar = pattern[patternIndex + 2];
                
                if (textChar >= startChar && textChar <= endChar)
                    matched = true;
                
                patternIndex += 3;
            }
            else
            {
                // Single character
                if (textChar == patternChar)
                    matched = true;
                
                patternIndex++;
            }
        }

        if (patternIndex >= pattern.Length) // Missing closing ]
            return false;

        patternIndex++; // Skip closing ]
        nextPatternIndex = patternIndex;

        return negated ? !matched : matched;
    }
        
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
    public static Func<string, bool> Combine(params Func<string, bool>[] predicates)
    {
        if (predicates.Length == 0)
            return includeAllFilter;
        
        if (predicates.Length == 1)
            return predicates[0];

        return path =>
        {
            foreach (var predicate in predicates)
            {
                if (!predicate(path))
                    return false;
            }
            return true;
        };
    }
        
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates a common pattern-based filter function with the specified filtering mode.
    /// </summary>
    /// <param name="patterns">The patterns to process.</param>
    /// <param name="mode">The filtering mode that determines how patterns are interpreted and applied.</param>
    /// <returns>A predicate function that applies the pattern logic according to the specified mode.</returns>
    /// <remarks>
    /// This method provides the core pattern matching logic used by both ignore-style and include-style filters.
    /// The mode parameter determines whether patterns act as an exclusion list (Ignore mode) or inclusion list (Include mode).
    /// </remarks>
    private static Func<string, bool> CreateCommonPatternFilter(
        string[] patterns, PatternFilterMode mode)
    {
        // Determine behavior based on mode
        Func<string, bool> emptyPatternsFilter;
        bool defaultValue;
        bool normalPatternValue;
        bool negationPatternValue;
        switch (mode)
        {
            case PatternFilterMode.Ignore:
                // Ignore mode: default=include, match=exclude, negation=include
                emptyPatternsFilter = includeAllFilter;
                defaultValue = true;
                normalPatternValue = false;
                negationPatternValue = true;
                break;
            case PatternFilterMode.Include:
                // Include mode: default=exclude, match=include, negation=exclude
                emptyPatternsFilter = ignoreAllFilter;
                defaultValue = false;
                normalPatternValue = true;
                negationPatternValue = false;
                break;
            default:
                throw new InvalidOperationException();
        }

        // Check if patterns collection is empty
        if (patterns.Length == 0)
        {
            return emptyPatternsFilter;
        }

        return path =>
        {
            bool? decision = null;

            foreach (var pattern in patterns)
            {
                var isNegationPattern = pattern.StartsWith("!");
                var actualPattern = isNegationPattern ? pattern.Substring(1) : pattern;
                
                if (IsMatch(path, actualPattern))
                {
                    decision = isNegationPattern ? negationPatternValue : normalPatternValue;
                }
            }

            return decision ?? defaultValue;
        };
    }

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
    public static Func<string, bool> CreateIgnoreFilter(string[] excludePatterns) =>
        CreateCommonPatternFilter(excludePatterns, PatternFilterMode.Ignore);

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
    public static Func<string, bool> CreateIncludeFilter(string[] includePatterns) =>
       CreateCommonPatternFilter(includePatterns, PatternFilterMode.Include);

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
        commonIgnoreFilter;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
    public static async ValueTask<Func<string, bool>> CreateFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#else
    public static async Task<Func<string, bool>> CreateFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#endif
    {
        var patterns = new List<string>();
        var reader = new AsyncTextReader(gitignoreStream);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
                break;

            // Trim whitespace
            line = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            patterns.Add(line);
        }

        return CreateCommonPatternFilter(patterns.ToArray(), PatternFilterMode.Ignore);
    }
}
