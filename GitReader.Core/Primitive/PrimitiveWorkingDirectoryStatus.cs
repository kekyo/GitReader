////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using System;

namespace GitReader.Primitive;

/// <summary>
/// Represents the status of files in a Git working directory, categorized by their staging status.
/// </summary>
public readonly struct PrimitiveWorkingDirectoryStatus : IEquatable<PrimitiveWorkingDirectoryStatus>
{
    /// <summary>
    /// Files that have been staged for commit.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> StagedFiles;
    
    /// <summary>
    /// Files that have unstaged changes in the working directory.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> UnstagedFiles;
    
    /// <summary>
    /// Files that are not tracked by Git.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> UntrackedFiles;

    /// <summary>
    /// Initializes a new instance of the PrimitiveWorkingDirectoryStatus structure.
    /// </summary>
    /// <param name="stagedFiles">Files that have been staged for commit.</param>
    /// <param name="unstagedFiles">Files that have unstaged changes in the working directory.</param>
    /// <param name="untrackedFiles">Files that are not tracked by Git.</param>
    public PrimitiveWorkingDirectoryStatus(
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> stagedFiles,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> unstagedFiles,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> untrackedFiles)
    {
        this.StagedFiles = stagedFiles;
        this.UnstagedFiles = unstagedFiles;
        this.UntrackedFiles = untrackedFiles;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveWorkingDirectoryStatus is equal to the current instance.
    /// </summary>
    /// <param name="rhs">The PrimitiveWorkingDirectoryStatus to compare with the current instance.</param>
    /// <returns>true if the specified PrimitiveWorkingDirectoryStatus is equal to the current instance; otherwise, false.</returns>
    public bool Equals(PrimitiveWorkingDirectoryStatus rhs) =>
        this.StagedFiles.Equals(rhs.StagedFiles) &&
        this.UnstagedFiles.Equals(rhs.UnstagedFiles) &&
        this.UntrackedFiles.Equals(rhs.UntrackedFiles);

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
            var hashCode = this.StagedFiles.GetHashCode();
            hashCode = (hashCode * 397) ^ this.UnstagedFiles.GetHashCode();
            hashCode = (hashCode * 397) ^ this.UntrackedFiles.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string that represents the current instance.
    /// </summary>
    /// <returns>A string that represents the current instance.</returns>
    public override string ToString() =>
        $"Staged: {this.StagedFiles.Count}, Unstaged: {this.UnstagedFiles.Count}, Untracked: {this.UntrackedFiles.Count}";
} 