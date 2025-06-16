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

/// <summary>
/// Represents a Git branch reference.
/// </summary>
public sealed class Branch :
    IEquatable<Branch>, IInternalCommitReference
{
    private readonly WeakReference rwr;

    /// <summary>
    /// The hash of the commit that this branch points to.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Head;

    /// <summary>
    /// The name of the branch.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// Indicates whether this is a remote branch.
    /// </summary>
    public readonly bool IsRemote;

    internal Branch(
        WeakReference rwr,
        string name,
        Hash head,
        bool isRemote)
    {
        this.rwr = rwr;
        this.Head = head;
        this.Name = name;
        this.IsRemote = isRemote;
    }

    Hash ICommitReference.Hash =>
        this.Head;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    /// <summary>
    /// Determines whether the specified Branch is equal to the current Branch.
    /// </summary>
    /// <param name="rhs">The Branch to compare with the current Branch.</param>
    /// <returns>true if the specified Branch is equal to the current Branch; otherwise, false.</returns>
    public bool Equals(Branch rhs) =>
        rhs is { } &&
        this.Name.Equals(rhs.Name) &&
        this.Head.Equals(rhs.Head) &&
        this.IsRemote == rhs.IsRemote;

    bool IEquatable<Branch>.Equals(Branch? rhs) =>
        this.Equals(rhs!);

    /// <summary>
    /// Determines whether the specified object is equal to the current Branch.
    /// </summary>
    /// <param name="obj">The object to compare with the current Branch.</param>
    /// <returns>true if the specified object is equal to the current Branch; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Branch rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Head.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IsRemote.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the branch.
    /// </summary>
    /// <returns>A string representation of the branch.</returns>
    public override string ToString() =>
        $"{this.Head}: {this.Name}{(this.IsRemote ? " [remote]" : "")}";
}
