////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

//
// Glob pattern matching tests based on official specifications:
// 
// 1. Git .gitignore specification:
//    https://git-scm.com/docs/gitignore
//
// 2. fnmatch(3) specification (referenced by gitignore):
//    https://man7.org/linux/man-pages/man3/fnmatch.3.html
//    https://pubs.opengroup.org/onlinepubs/9699919799/functions/fnmatch.html
//
// 3. IEEE Std 1003.1-2017 (POSIX.1-2017) fnmatch specification:
//    https://pubs.opengroup.org/onlinepubs/9699919799/utilities/V3_chap02.html#tag_18_13
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitReader.IO;

namespace GitReader.Internal;

/// <summary>
/// Provides glob pattern matching functionality for .gitignore files.
/// </summary>
internal static class Glob
{
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
        ".DS_Store", "Thumbs.db", "Desktop.ini",
        // Version control files
        "*.orig", "*.rej"
    ];
    
    internal static readonly GlobFilter excludeAllFilter = (_, _) => GlobFilterStates.Exclude;
    internal static readonly GlobFilter nothingFilter = (initialState, _) => initialState;
    internal static readonly GlobFilter commonIgnoreFilter = CreateExcludePatternFilter(commonPatterns);

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

        var result = IsMatchInternal(path, pattern);
        
        return isNegated ? !result : result;
    }

    /// <summary>
    /// Internal method that performs pattern matching without handling negation or comments.
    /// Used by CreateCommonPatternFilter to avoid double negation processing.
    /// </summary>
    private static bool IsMatchInternal(string path, string pattern)
    {
        // Normalize path separators to forward slashes (only for path, not pattern)
        path = path.Replace('\\', '/');
        
        // Remove trailing slashes from path
        path = path.TrimEnd('/');

        // Normalize consecutive slashes in pattern to single slash
        while (pattern.Contains("//"))
        {
            pattern = pattern.Replace("//", "/");
        }

        return MatchesPattern(path, pattern);
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
    /// Combines multiple glob filter predicates into a single glob filter predicate that evaluates them in order.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine, in evaluation order.</param>
    /// <returns>A combined predicate that evaluates predicates in order and uses the first definitive result.</returns>
    /// <example>
    /// var parentFilter = Glob.CreateExcludeFilter("*.log");
    /// var childFilter = Glob.CreateExcludeFilter("!important.log");
    /// var combined = Glob.Combine(parentFilter, childFilter);   // childFilter can override others
    /// </example>
    public static GlobFilter Combine(GlobFilter[] predicates)
    {
        switch (predicates.Length)
        {
            // If no predicates, return nothing filter
            case 0:
                return nothingFilter;
            // If only one predicate, return it
            case 1:
                return predicates[0];
            default:
                // Return a combined predicate that evaluates predicates in order
                return (initialState, path) => predicates.Aggregate(
                    initialState,
                    (currentState, predicate) => predicate(currentState, path));
        }
    }

    /// <summary>
    /// Create exclude pattern-based glob filter function
    /// </summary>
    /// <param name="excludePatterns">The patterns to process.</param>
    /// <returns>A glob filter function that applies the exclude pattern logic</returns>
    private static GlobFilter CreateExcludePatternFilter(
        string[] excludePatterns)
    {
        // Glob filter for a exclude pattern
        static GlobFilterStates RunFilterOnce(GlobFilterStates state, string excludePattern, string path)
        {
            // Check if pattern is a negation pattern
            // "Negation" does not mean "include", it means "not exclude"
            var isNegationPattern = excludePattern.StartsWith("!");
            var actualPattern = isNegationPattern ? excludePattern.Substring(1) : excludePattern;

            // When path matches pattern
            return IsMatchInternal(path, actualPattern) ?
                // When pattern is a negation pattern, return neutral (NotExclude)
                (isNegationPattern ? GlobFilterStates.NotExclude : GlobFilterStates.Exclude) :
                state;
        }

        switch (excludePatterns.Length)
        {
            // If no patterns, return nothing filter
            case 0:
                return nothingFilter;
            // Application fot only once
            case 1:
                return (initialState, path) => RunFilterOnce(initialState, excludePatterns[0], path);
            // Application for all patterns
            default:
                return (initialState, path) => excludePatterns.Aggregate(
                    initialState,
                    (currentState, excludePattern) => RunFilterOnce(currentState, excludePattern, path));
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates a glob path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that excludes files matching any of the provided .gitignore-style patterns.
    /// </summary>
    /// <param name="excludePatterns">Array of .gitignore-style patterns to exclude files.</param>
    /// <returns>A predicate function that returns Exclude if the path should be excluded, or Neutral if undecided.</returns>
    /// <example>
    /// var filter = Glob.CreateExcludeFilter(new[] { "*.log", "bin/", "obj/", "node_modules/" });
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static GlobFilter CreateExcludeFilter(string[] excludePatterns) =>
        CreateExcludePatternFilter(excludePatterns);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <example>
    /// using var stream = File.OpenRead(".gitignore");
    /// var filter = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<GlobFilter> CreateExcludeFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#else
    public static async Task<GlobFilter> CreateExcludeFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#endif
    {
        var excludePatterns = new List<string>();
        var reader = new AsyncTextReader(gitignoreStream);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            // Trim whitespace
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (Utilities.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            excludePatterns.Add(trimmed);
        }

        return CreateExcludePatternFilter(excludePatterns.ToArray());
    }

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreReader">Text reader containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <example>
    /// using var reader = File.OpenText(".gitignore");
    /// var filter = await Glob.CreateExcludeFilterFromGitignoreAsync(reader, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<GlobFilter> CreateExcludeFilterFromGitignoreAsync(
        TextReader gitignoreReader, CancellationToken ct = default)
#else
    public static async Task<GlobFilter> CreateExcludeFilterFromGitignoreAsync(
        TextReader gitignoreReader, CancellationToken ct = default)
#endif
    {
        var excludePatterns = new List<string>();

        while (true)
        {
            var line = await gitignoreReader.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                break;
            }

            // Trim whitespace
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (Utilities.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            excludePatterns.Add(trimmed);
        }

        return CreateExcludePatternFilter(excludePatterns.ToArray());
    }

    /// <summary>
    /// Creates a glob path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreLines">Text lines containing .gitignore content.</param>
    /// <returns>A predicate function that returns Exclude if excluded, or NotExclude if unevaluated.</returns>
    /// <remarks>Difference from CreateExcludeFilter() is that empty lines and comments are evaluated.</remarks>
    public static GlobFilter CreateExcludeFilterFromGitignore(
        IEnumerable<string> gitignoreLines)
    {
        var excludePatterns = new List<string>();
        
        foreach (var line in gitignoreLines)
        {
            // Trim whitespace
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (Utilities.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            excludePatterns.Add(trimmed);
        }

        return CreateExcludePatternFilter(excludePatterns.ToArray());
    }
}
