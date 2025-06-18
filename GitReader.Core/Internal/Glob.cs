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

    internal static readonly Func<string, bool> includeAll = _ => true;
    internal static readonly Func<string, bool> ignoreAll = _ => false;

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
    public static Func<string, bool> CreateIgnoreFilter(params string[] excludePatterns)
    {
        if (excludePatterns.Length == 0)
        {
            // Include all files if no excluding patterns provided
            return includeAll;
        }

        return path =>
        {
            foreach (var pattern in excludePatterns)
            {
                if (IsMatch(path, pattern))
                {
                    return false; // Exclude this file
                }
            }
            return true; // Include this file
        };
    }

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
    public static Func<string, bool> CreateIncludeFilter(params string[] includePatterns)
    {
        if (includePatterns.Length == 0)
        {
            // Exclude all files if no patterns provided
            return ignoreAll;
        }

        return path =>
        {
            foreach (var pattern in includePatterns)
            {
                if (IsMatch(path, pattern))
                {
                    return true; // Include this file
                }
            }
            return false; // Exclude this file
        };
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

    /// <summary>
    /// Creates a path filter predicate for use with GetWorkingDirectoryStatusAsync() 
    /// that applies standard .gitignore patterns commonly used in development projects.
    /// This includes patterns for build outputs, dependencies, logs, and temporary files.
    /// </summary>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// var filter = Glob.CreateCommonIgnoreFilter();
    /// var status = await repository.GetWorkingDirectoryStatusAsync(filter);
    /// </example>
    public static Func<string, bool> CreateCommonIgnoreFilter() =>
        CreateIgnoreFilter(commonPatterns);

    /// <summary>
    /// Creates a path filter predicate from a .gitignore stream.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A predicate function that returns true if the path should be included (not ignored).</returns>
    /// <example>
    /// using var stream = File.OpenRead(".gitignore");
    /// var filter = await Glob.CreateGitignoreFilterAsync(stream, ct);
    /// var shouldInclude = filter("somefile.txt");
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<Func<string, bool>> CreateGitignoreFilterAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#else
    public static async Task<Func<string, bool>> CreateGitignoreFilterAsync(
        Stream gitignoreStream, CancellationToken ct = default)
#endif
    {
        var patterns = new List<string>();
        var reader = new AsyncTextReader(gitignoreStream);

        try
        {
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
        }
        catch
        {
            // If reading fails, return a filter that includes everything
            return includeAll;
        }

        if (patterns.Count == 0)
        {
            return includeAll;
        }

        return path =>
        {
            // Process .gitignore patterns in order - later patterns can override earlier ones
            bool? gitignoreDecision = null;

            foreach (var pattern in patterns)
            {
                bool isNegationPattern = pattern.StartsWith("!");
                string actualPattern = isNegationPattern ? pattern.Substring(1) : pattern;
                
                // Use IsMatch with the actual pattern (without !)
                if (IsMatch(path, actualPattern))
                {
                    if (isNegationPattern)
                    {
                        // This is a negation pattern that matched - explicitly include
                        gitignoreDecision = true;
                    }
                    else
                    {
                        // This is a normal pattern that matched - explicitly exclude
                        gitignoreDecision = false;
                    }
                }
            }

            // If .gitignore made an explicit decision, use it
            if (gitignoreDecision.HasValue)
            {
                return gitignoreDecision.Value;
            }

            // If no patterns matched, include the file
            return true;
        };
    }

    /// <summary>
    /// Combines a .gitignore filter with an existing base filter.
    /// </summary>
    /// <param name="gitignoreStream">Stream containing .gitignore content.</param>
    /// <param name="baseFilter">The base filter to combine with. If null, defaults to include all.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A combined predicate function that applies both filters.</returns>
    /// <example>
    /// var baseFilter = Glob.CreateCommonIgnoreFilter();
    /// using var stream = File.OpenRead(".gitignore");
    /// var combinedFilter = await Glob.CombineWithGitignoreAsync(stream, baseFilter, ct);
    /// </example>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<Func<string, bool>> CombineWithGitignoreAsync(
        Stream gitignoreStream, 
        Func<string, bool>? baseFilter = null, 
        CancellationToken ct = default)
#else
    public static async Task<Func<string, bool>> CombineWithGitignoreAsync(
        Stream gitignoreStream, 
        Func<string, bool>? baseFilter = null, 
        CancellationToken ct = default)
#endif
    {
        baseFilter ??= includeAll;

        // Create a combined filter that processes gitignore patterns with base filter
        var patterns = new List<string>();
        var reader = new AsyncTextReader(gitignoreStream);

        try
        {
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
        }
        catch
        {
            // If reading fails, return the base filter
            return baseFilter;
        }

        if (patterns.Count == 0)
        {
            return baseFilter;
        }

        return path =>
        {
            // Process .gitignore patterns in order - later patterns can override earlier ones
            bool? gitignoreDecision = null;

            foreach (var pattern in patterns)
            {
                bool isNegationPattern = pattern.StartsWith("!");
                string actualPattern = isNegationPattern ? pattern.Substring(1) : pattern;
                
                // Use IsMatch with the actual pattern (without !)
                if (IsMatch(path, actualPattern))
                {
                    if (isNegationPattern)
                    {
                        // This is a negation pattern that matched - explicitly include
                        gitignoreDecision = true;
                    }
                    else
                    {
                        // This is a normal pattern that matched - explicitly exclude
                        gitignoreDecision = false;
                    }
                }
            }

            // If .gitignore made an explicit decision, use it
            if (gitignoreDecision.HasValue)
            {
                return gitignoreDecision.Value;
            }

            // Otherwise, delegate to base filter
            return baseFilter(path);
        };
    }
}

