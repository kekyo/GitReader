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

public readonly struct PrimitiveWorktree : IEquatable<PrimitiveWorktree>
{
    public readonly string Name;
    public readonly string Path;
    public readonly WorktreeStatus Status;
    
    // Internal fields for accessing repository and worktree directory
    internal readonly Repository Repository;
    internal readonly string? WorktreeDir;

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

    public bool IsMain => this.Name == "(main)";

    public bool Equals(PrimitiveWorktree rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status;

    bool IEquatable<PrimitiveWorktree>.Equals(PrimitiveWorktree rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveWorktree rhs && this.Equals(rhs);

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

    public override string ToString() =>
        $"{this.Name}: {this.Path} ({this.Status})";
} 