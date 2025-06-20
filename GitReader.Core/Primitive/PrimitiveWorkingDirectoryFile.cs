////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

/// <summary>
/// Represents a file in the working directory with its status and associated hashes.
/// </summary>
public readonly struct PrimitiveWorkingDirectoryFile : IEquatable<PrimitiveWorkingDirectoryFile>
{
    /// <summary>
    /// The path of the file relative to the repository root.
    /// </summary>
    public readonly string Path;
    
    /// <summary>
    /// The status of the file in the working directory.
    /// </summary>
    public readonly FileStatus Status;
    
    /// <summary>
    /// The hash of the file content in the index, if it exists.
    /// </summary>
    public readonly Hash? IndexHash;
    
    /// <summary>
    /// The hash of the file content in the working tree, if it exists.
    /// </summary>
    public readonly Hash? WorkingTreeHash;

    /// <summary>
    /// Initializes a new instance of the PrimitiveWorkingDirectoryFile structure.
    /// </summary>
    /// <param name="path">The path of the file relative to the repository root.</param>
    /// <param name="status">The status of the file in the working directory.</param>
    /// <param name="indexHash">The hash of the file content in the index, if it exists.</param>
    /// <param name="workingTreeHash">The hash of the file content in the working tree, if it exists.</param>
    public PrimitiveWorkingDirectoryFile(
        string path,
        FileStatus status,
        Hash? indexHash,
        Hash? workingTreeHash)
    {
        this.Path = path;
        this.Status = status;
        this.IndexHash = indexHash;
        this.WorkingTreeHash = workingTreeHash;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveWorkingDirectoryFile is equal to the current instance.
    /// </summary>
    /// <param name="rhs">The PrimitiveWorkingDirectoryFile to compare with the current instance.</param>
    /// <returns>true if the specified PrimitiveWorkingDirectoryFile is equal to the current instance; otherwise, false.</returns>
    public bool Equals(PrimitiveWorkingDirectoryFile rhs) =>
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status &&
        this.IndexHash.Equals(rhs.IndexHash) &&
        this.WorkingTreeHash.Equals(rhs.WorkingTreeHash);

    bool IEquatable<PrimitiveWorkingDirectoryFile>.Equals(PrimitiveWorkingDirectoryFile rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveWorkingDirectoryFile rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Path.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Status.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IndexHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.WorkingTreeHash.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string that represents the current instance.
    /// </summary>
    /// <returns>A string that represents the current instance.</returns>
    public override string ToString() =>
        $"{this.Status}: {this.Path}";
} 