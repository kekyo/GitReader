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
/// Represents a Git worktree.
/// </summary>
public readonly struct Worktree
{
    /// <summary>
    /// Gets the name of the worktree.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// Gets the path to the worktree directory.
    /// </summary>
    public readonly string Path;
    
    /// <summary>
    /// Gets the hash of the current HEAD commit, if available.
    /// </summary>
    public readonly Hash? Head;
    
    /// <summary>
    /// Gets the name of the branch currently checked out in this worktree, if any.
    /// </summary>
    public readonly string? Branch;
    
    /// <summary>
    /// Gets the status of the worktree.
    /// </summary>
    public readonly WorktreeStatus Status;

    internal Worktree(
        string name,
        string path,
        Hash? head,
        string? branch,
        WorktreeStatus status)
    {
        this.Name = name;
        this.Path = path;
        this.Head = head;
        this.Branch = branch;
        this.Status = status;
    }

    /// <summary>
    /// Gets a value indicating whether this is the main worktree.
    /// </summary>
    public bool IsMain => this.Name == "(main)";

    /// <summary>
    /// Determines whether the specified object is equal to the current Worktree.
    /// </summary>
    /// <param name="obj">The object to compare with the current Worktree.</param>
    /// <returns>true if the specified object is equal to the current Worktree; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Worktree rhs &&
        this.Name.Equals(rhs.Name) &&
        this.Path.Equals(rhs.Path) &&
        this.Head.Equals(rhs.Head) &&
        (this.Branch?.Equals(rhs.Branch) ?? rhs.Branch == null) &&
        this.Status == rhs.Status;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Path.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Head.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Branch?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ this.Status.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the worktree.
    /// </summary>
    /// <returns>A string representation of the worktree.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.Path} ({this.Status})";
} 