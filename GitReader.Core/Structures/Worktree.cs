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

namespace GitReader.Structures;

public sealed class Worktree : IRepositoryReference
{
    private readonly WeakReference rwr;

    internal Worktree(
        WeakReference rwr,
        string name,
        string path,
        Hash? head,
        string? branch,
        WorktreeStatus status)
    {
        this.rwr = rwr;
        this.Name = name;
        this.Path = path;
        this.Head = head;
        this.Branch = branch;
        this.Status = status;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public string Name { get; }
    public string Path { get; }
    public Hash? Head { get; }
    public string? Branch { get; }
    public WorktreeStatus Status { get; }

    public bool IsMain => this.Name == "(main)";

    public override bool Equals(object? obj) =>
        obj is Worktree rhs &&
        this.Name.Equals(rhs.Name) &&
        this.Path.Equals(rhs.Path) &&
        this.Head.Equals(rhs.Head) &&
        (this.Branch?.Equals(rhs.Branch) ?? rhs.Branch == null) &&
        this.Status == rhs.Status;

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

    public override string ToString() =>
        $"{this.Name}: {this.Path} ({this.Status})";
} 