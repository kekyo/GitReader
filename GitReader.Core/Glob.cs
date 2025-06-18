////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

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
    public static bool IsMatch(string path, string pattern)
    {
        if (path == null || string.IsNullOrEmpty(pattern))
            return false;

        // Handle negation pattern
        bool isNegated = false;
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

        bool result = MatchesPattern(path, pattern);
        
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
            return MatchGlob(path, pattern) ||
                   (directoryOnly && path.Length == 0); // Root directory case
        }

        // For relative patterns, check if pattern matches at any level
        if (pattern.Contains("/"))
        {
            // Pattern contains path separator - match from current level
            return MatchGlob(path, pattern);
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
} 