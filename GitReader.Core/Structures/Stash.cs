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
/// Represents a Git stash entry.
/// </summary>
public sealed class Stash :
    IEquatable<Stash>, IInternalCommitReference
{
    private readonly WeakReference rwr;

    /// <summary>
    /// The hash identifier of this stash commit.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Hash;

    /// <summary>
    /// The signature of the person who created this stash.
    /// </summary>
    public readonly Signature Committer;
    
    /// <summary>
    /// The message describing this stash.
    /// </summary>
    public readonly string Message;

    /// <summary>
    /// Initializes a new instance of the Stash class.
    /// </summary>
    /// <param name="rwr">Weak reference to the repository.</param>
    /// <param name="hash">The hash identifier of this stash commit.</param>
    /// <param name="committer">The signature of the person who created this stash.</param>
    /// <param name="message">The message describing this stash.</param>
    internal Stash(
        WeakReference rwr, Hash hash, Signature committer, string message)
    {
        this.rwr = rwr;
        this.Hash = hash;
        this.Committer = committer;
        this.Message = message;
    }

    Hash ICommitReference.Hash =>
        this.Hash;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    /// <summary>
    /// Determines whether the specified Stash is equal to the current Stash.
    /// </summary>
    /// <param name="rhs">The Stash to compare with the current Stash.</param>
    /// <returns>true if the specified Stash is equal to the current Stash; otherwise, false.</returns>
    public bool Equals(Stash rhs) =>
        rhs is { } &&
        (object.ReferenceEquals(this, rhs) ||
            (this.Hash.Equals(rhs.Hash) &&
             this.Committer.Equals(rhs.Committer) &&
             this.Message.Equals(rhs.Message)));

    bool IEquatable<Stash>.Equals(Stash? rhs) =>
        this.Equals(rhs!);

    /// <summary>
    /// Determines whether the specified object is equal to the current Stash.
    /// </summary>
    /// <param name="obj">The object to compare with the current Stash.</param>
    /// <returns>true if the specified object is equal to the current Stash; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Stash rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the stash.
    /// </summary>
    /// <returns>A string representation of the stash.</returns>
    public override string ToString() =>
        $"{this.Hash}: {this.Committer}: {this.Message}";
}