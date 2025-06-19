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
/// Represents a Git worktree with its name, path, and status.
/// </summary>
public readonly struct PrimitiveWorktree : IEquatable<PrimitiveWorktree>
{
    /// <summary>
    /// The name of the worktree.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The path to the worktree directory.
    /// </summary>
    public readonly string Path;
    
    /// <summary>
    /// The status of the worktree.
    /// </summary>
    public readonly WorktreeStatus Status;
    
    // Internal fields for accessing repository and worktree directory
    internal readonly Repository Repository;
    internal readonly string? WorktreeDir;

    /// <summary>
    /// Initializes a new instance of the PrimitiveWorktree structure.
    /// </summary>
    /// <param name="repository">The repository instance.</param>
    /// <param name="name">The name of the worktree.</param>
    /// <param name="path">The path to the worktree directory.</param>
    /// <param name="status">The status of the worktree.</param>
    /// <param name="worktreeDir">The worktree directory path (optional).</param>
    internal PrimitiveWorktree(
        Repository repository,
        string name,
        string path,
        WorktreeStatus status,
        string? worktreeDir = null)
    {
        this.Repository = repository;
        this.Name = name;
        this.Path = path;
        this.Status = status;
        this.WorktreeDir = worktreeDir;
    }

    /// <summary>
    /// Gets a value indicating whether this is the main worktree.
    /// </summary>
    public bool IsMain => this.Name == "(main)";

    /// <summary>
    /// Determines whether the specified PrimitiveWorktree is equal to the current instance.
    /// </summary>
    /// <param name="rhs">The PrimitiveWorktree to compare with the current instance.</param>
    /// <returns>true if the specified PrimitiveWorktree is equal to the current instance; otherwise, false.</returns>
    public bool Equals(PrimitiveWorktree rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status;

    bool IEquatable<PrimitiveWorktree>.Equals(PrimitiveWorktree rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveWorktree rhs && this.Equals(rhs);

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
            hashCode = (hashCode * 397) ^ this.Status.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string that represents the current instance.
    /// </summary>
    /// <returns>A string that represents the current instance.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.Path} ({this.Status})";
} 