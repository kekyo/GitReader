////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures;

/// <summary>
/// Represents a file in the working directory with its status information.
/// </summary>
public readonly struct WorkingDirectoryFile
{
    /// <summary>
    /// Gets the path of the file relative to the repository root.
    /// </summary>
    public readonly string Path;
    
    /// <summary>
    /// Gets the status of the file.
    /// </summary>
    public readonly FileStatus Status;
    
    /// <summary>
    /// Gets the hash of the file in the index, if available.
    /// </summary>
    public readonly Hash? IndexHash;

    /// <summary>
    /// Gets the hash of the file in the working tree, if available.
    /// </summary>
    public readonly Hash? WorkingTreeHash;

    internal WorkingDirectoryFile(
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
    /// Determines whether the specified object is equal to the current WorkingDirectoryFile.
    /// </summary>
    /// <param name="obj">The object to compare with the current WorkingDirectoryFile.</param>
    /// <returns>true if the specified object is equal to the current WorkingDirectoryFile; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is WorkingDirectoryFile rhs &&
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status &&
        this.IndexHash.Equals(rhs.IndexHash) &&
        this.WorkingTreeHash.Equals(rhs.WorkingTreeHash);

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
    /// Returns a string representation of the working directory file.
    /// </summary>
    /// <returns>A string representation of the working directory file.</returns>
    public override string ToString() =>
        $"{this.Status}: {this.Path}";
}
