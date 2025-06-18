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
    /// <summary>
    /// Specifies the behavior mode for pattern-based filtering.
    /// </summary>
    private enum PatternFilterMode
    {
        /// <summary>
        /// Exclude mode: Files matching patterns are excluded, non-matching files are neutral.
        /// This is the standard .gitignore behavior where patterns specify what to exclude.
        /// </summary>
        Exclude,

        /// <summary>
        /// Include mode: Only files matching patterns are included, non-matching files are neutral.
        /// This is useful for allowlist-style filtering where patterns specify what to include.
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
        ".DS_Store", "Thumbs.db", "Desktop.ini",
        // Version control files
        "*.orig", "*.rej"
    ];
    
    internal static readonly FilterDecisionDelegate includeAllFilter = (_, _) => FilterDecision.Include;
    internal static readonly FilterDecisionDelegate excludeAllFilter = (_, _) => FilterDecision.Exclude;
    internal static readonly FilterDecisionDelegate neutralFilter = (_, _) => FilterDecision.Neutral;
    internal static readonly FilterDecisionDelegate commonIgnoreFilter =
        CreateCommonPatternFilter(commonPatterns, PatternFilterMode.Exclude);

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
    /// Combines multiple predicates into a single predicate that evaluates them in order.
    /// The first definitive result is returned.
    /// </summary>
    /// <param name="predicates">The predicate functions to combine, in evaluation order.</param>
    /// <returns>A combined predicate that evaluates predicates in order and uses the first definitive result.</returns>
    /// <example>
    /// var parentFilter = Glob.CreateExcludeFilter("*.log");
    /// var childFilter = Glob.CreateIncludeFilter("important.log");
    /// var combined = Glob.Combine(childFilter, parentFilter);  // childFilter can override parentFilter
    /// </example>
    public static FilterDecisionDelegate Combine(FilterDecisionDelegate[] predicates)
    {
        switch (predicates.Length)
        {
            // If no predicates, return neutral (no decision)
            case 0:
                return neutralFilter;
            // If only one predicate, return it
            case 1:
                return predicates[0];
            default:
                var reversedPredicates = predicates.Reverse().ToArray();

                // Return a combined predicate that evaluates predicates in reverse order
                return (path, initialDecision) =>
                {
                    var currentDecision = initialDecision;
            
                    // Evaluate predicates in reversed order
                    foreach (var predicate in reversedPredicates)
                    {
                        // Evaluate
                        currentDecision = predicate(path, currentDecision);
                    }

                    return currentDecision;
                };
        }
    }

    /// <summary>
    /// Creates a common pattern-based filter function with the specified filtering mode.
    /// </summary>
    /// <param name="patterns">The patterns to process.</param>
    /// <param name="mode">The filtering mode that determines how patterns are interpreted and applied.</param>
    /// <returns>A predicate function that applies the pattern logic according to the specified mode.</returns>
    private static FilterDecisionDelegate CreateCommonPatternFilter(
        string[] patterns, PatternFilterMode mode)
    {
        // Check if patterns collection is empty
        if (patterns.Length == 0)
        {
            return neutralFilter;
        }

        // Determine behavior based on mode
        var matchPatternValue = mode == PatternFilterMode.Exclude ?
            FilterDecision.Exclude : FilterDecision.Include;

        return (path, initialDecision) =>
        {
            var currentDecision = initialDecision;
            
            // NOTE: Maybe better to use reverse order.
            // In that case, we can create a state to "ignore the next decision" when a negation pattern is detected.
            // This way, we can return the decision immediately when a normal pattern is detected.

            // Evaluate patterns in reverse order
            foreach (var pattern in patterns)
            {
                // Check if pattern is a negation pattern
                var isNegationPattern = pattern.StartsWith("!");
                var actualPattern = isNegationPattern ? pattern.Substring(1) : pattern;

                // When path matches pattern
                if (IsMatchInternal(path, actualPattern))
                {
                    // When pattern is a negation pattern, return neutral
                    currentDecision = isNegationPattern ? FilterDecision.Neutral : matchPatternValue;
                }
            }

            // Return the result of the pattern immediately
            return currentDecision;
        };
    }
        
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
    public static FilterDecisionDelegate CreateExcludeFilter(string[] excludePatterns) =>
        CreateCommonPatternFilter(excludePatterns, PatternFilterMode.Exclude);

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
    public static FilterDecisionDelegate CreateIncludeFilter(string[] includePatterns) =>
       CreateCommonPatternFilter(includePatterns, PatternFilterMode.Include);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns Include if the path should be included, Exclude if excluded, or Neutral if undecided.</returns>
    /// <example>
    /// using var stream = File.OpenRead(".gitignore");
    /// var filter = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<FilterDecisionDelegate> CreateExcludeFilterFromGitignoreAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#else
    public static async Task<FilterDecisionDelegate> CreateExcludeFilterFromGitignoreAsync(
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

        return CreateCommonPatternFilter(patterns.ToArray(), PatternFilterMode.Exclude);
    }
}
