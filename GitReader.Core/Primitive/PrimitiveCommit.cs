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
using System.Linq;

namespace GitReader.Primitive;

/// <summary>
/// Represents a primitive Git commit with its basic properties.
/// </summary>
public readonly struct PrimitiveCommit : IEquatable<PrimitiveCommit>
{
    /// <summary>
    /// The hash of the commit.
    /// </summary>
    public readonly Hash Hash;

    /// <summary>
    /// The hash of the tree root associated with this commit.
    /// </summary>
    public readonly Hash TreeRoot;

    /// <summary>
    /// The author signature of the commit.
    /// </summary>
    public readonly Signature Author;

    /// <summary>
    /// The committer signature of the commit.
    /// </summary>
    public readonly Signature Committer;

    /// <summary>
    /// The hashes of the parent commits.
    /// </summary>
    public readonly ReadOnlyArray<Hash> Parents;

    /// <summary>
    /// The commit message.
    /// </summary>
    public readonly string Message;

    /// <summary>
    /// Initializes a new instance of the PrimitiveCommit struct.
    /// </summary>
    /// <param name="hash">The commit hash.</param>
    /// <param name="treeRoot">The tree root hash.</param>
    /// <param name="author">The author signature.</param>
    /// <param name="committer">The committer signature.</param>
    /// <param name="parents">The parent commit hashes.</param>
    /// <param name="message">The commit message.</param>
    public PrimitiveCommit(
        Hash hash,
        Hash treeRoot,
        Signature author,
        Signature committer,
        ReadOnlyArray<Hash> parents,
        string message)
    {
        this.Hash = hash;
        this.TreeRoot = treeRoot;
        this.Author = author;
        this.Committer = committer;
        this.Parents = parents;
        this.Message = message;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveCommit is equal to the current PrimitiveCommit.
    /// </summary>
    /// <param name="rhs">The PrimitiveCommit to compare with the current PrimitiveCommit.</param>
    /// <returns>true if the specified PrimitiveCommit is equal to the current PrimitiveCommit; otherwise, false.</returns>
    public bool Equals(PrimitiveCommit rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.TreeRoot.Equals(rhs.TreeRoot) &&
        this.Author.Equals(rhs.Author) &&
        this.Committer.Equals(rhs.Committer) &&
        this.Parents.SequenceEqual(rhs.Parents) &&
        this.Message.Equals(rhs.Message);

    bool IEquatable<PrimitiveCommit>.Equals(PrimitiveCommit rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveCommit.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveCommit.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveCommit; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveCommit rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.TreeRoot.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Author.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return this.Parents.Aggregate(
                hashCode,
                (agg, v) =>
                {
                    unchecked
                    {
                        return (agg * 397) ^ v.GetHashCode();
                    }
                });
        }
    }

    /// <summary>
    /// Returns a string representation of the primitive commit.
    /// </summary>
    /// <returns>A string representation of the primitive commit.</returns>
    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Message.Replace('\n', ' ')}";

    /// <summary>
    /// Implicitly converts a PrimitiveCommit to its Hash.
    /// </summary>
    /// <param name="commit">The PrimitiveCommit to convert.</param>
    /// <returns>The Hash of the commit.</returns>
    public static implicit operator Hash(PrimitiveCommit commit) =>
        commit.Hash;
}
