////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Linq;
using GitReader.Collections;
using GitReader.Internal;

namespace GitReader.Primitive;

/// <summary>
/// Represents the status of files in a Git working directory, categorized by their staging status.
/// </summary>
public readonly struct PrimitiveWorkingDirectoryStatus : IEquatable<PrimitiveWorkingDirectoryStatus>
{
    internal readonly string workingDirectoryPath;
    
    /// <summary>
    /// Files that have been staged for commit.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> StagedFiles;

    /// <summary>
    /// Files that have unstaged changes in the working directory.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> UnstagedFiles;

    /// <summary>
    /// The paths of the files that have been processed.
    /// </summary>
    /// <remarks>
    /// This is used to avoid processing the same file multiple times.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ReadOnlyArray<string> ProcessedPaths;

    /// <summary>
    /// Initializes a new instance of the PrimitiveWorkingDirectoryStatus structure.
    /// </summary>
    /// <param name="workingDirectoryPath">Working directory path.</param>
    /// <param name="stagedFiles">Files that have been staged for commit.</param>
    /// <param name="unstagedFiles">Files that have unstaged changes in the working directory.</param>
    /// <param name="processedPaths">The paths of the files that have been processed.</param>
    internal PrimitiveWorkingDirectoryStatus(
        string workingDirectoryPath,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> stagedFiles,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> unstagedFiles,
        ReadOnlyArray<string> processedPaths)
    {
        this.workingDirectoryPath = workingDirectoryPath;
        this.StagedFiles = stagedFiles;
        this.UnstagedFiles = unstagedFiles;
        this.ProcessedPaths = processedPaths;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveWorkingDirectoryStatus is equal to the current instance.
    /// </summary>
    /// <param name="rhs">The PrimitiveWorkingDirectoryStatus to compare with the current instance.</param>
    /// <returns>true if the specified PrimitiveWorkingDirectoryStatus is equal to the current instance; otherwise, false.</returns>
    public bool Equals(PrimitiveWorkingDirectoryStatus rhs) =>
        this.workingDirectoryPath.Equals(rhs.workingDirectoryPath) &&
        this.StagedFiles.CollectionEqual(rhs.StagedFiles) &&
        this.UnstagedFiles.CollectionEqual(rhs.UnstagedFiles) &&
        this.ProcessedPaths.CollectionEqual(rhs.ProcessedPaths);

    bool IEquatable<PrimitiveWorkingDirectoryStatus>.Equals(PrimitiveWorkingDirectoryStatus rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveWorkingDirectoryStatus rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.workingDirectoryPath.GetHashCode();
            hashCode = (hashCode * 397) ^ this.StagedFiles.Aggregate(0, (s, v) => (s * 397) ^ v.GetHashCode());
            hashCode = (hashCode * 397) ^ this.UnstagedFiles.Aggregate(0, (s, v) => (s * 397) ^ v.GetHashCode());
            hashCode = (hashCode * 397) ^ this.ProcessedPaths.Aggregate(0, (s, v) => (s * 397) ^ v.GetHashCode());
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string that represents the current instance.
    /// </summary>
    /// <returns>A string that represents the current instance.</returns>
    public override string ToString() =>
        $"Staged: {this.StagedFiles.Count}, Unstaged: {this.UnstagedFiles.Count}";
}
