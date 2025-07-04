﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using GitReader.Primitive;
using System;
using System.Linq;

namespace GitReader.Structures;

/// <summary>
/// Represents a Git commit object.
/// </summary>
public sealed class Commit :
    IEquatable<Commit>, IRepositoryReference
{
    private readonly WeakReference rwr;
    internal readonly ReadOnlyArray<Hash> parents;
    internal readonly string message;
    internal readonly Hash treeRoot;

    private ReadOnlyArray<Branch>? branches;
    private ReadOnlyArray<Tag>? tags;

    /// <summary>
    /// The hash identifier of this commit.
    /// </summary>
    public readonly Hash Hash;
    
    /// <summary>
    /// The author of this commit.
    /// </summary>
    public readonly Signature Author;
    
    /// <summary>
    /// The committer of this commit.
    /// </summary>
    public readonly Signature Committer;

    internal Commit(
        WeakReference rwr,
        PrimitiveCommit commit)
    {
        this.rwr = rwr;
        this.parents = commit.Parents;
        this.treeRoot = commit.TreeRoot;

        this.Hash = commit.Hash;
        this.Author = commit.Author;
        this.Committer = commit.Committer;
        this.message = commit.Message;
    }

    /// <summary>
    /// Gets the subject line of the commit message (first line).
    /// </summary>
    /// <remarks>
    /// The subject line is the first line of the commit message. It is nearly as git command `git log --format=%s`.
    /// </remarks>
    public string Subject
    {
        get
        {
            Utilities.CrackGitMessage(this.message, out var subject, out _);
            return subject;
        }
    }

    /// <summary>
    /// Gets the body of the commit message (everything after the first blank line).
    /// </summary>
    /// <remarks>
    /// The body is the rest of the commit message after the first blank line. It is nearly as git command `git log --format=%b`.
    /// </remarks>
    public string Body
    {
        get
        {
            Utilities.CrackGitMessage(this.message, out _, out var body);
            return body;
        }
    }

    /// <summary>
    /// Gets the branches that contain this commit.
    /// </summary>
    public ReadOnlyArray<Branch> Branches
    {
        get
        {
            if (this.branches == null)
            {
                // Beginning of race condition section,
                // but will discard dict later silently.
                this.branches = StructuredRepositoryFacade.GetRelatedBranches(this);
            }
            return this.branches;
        }
    }

    /// <summary>
    /// Gets the tags that point to this commit.
    /// </summary>
    public ReadOnlyArray<Tag> Tags
    {
        get
        {
            if (this.tags == null)
            {
                // Beginning of race condition section,
                // but will discard dict later silently.
                this.tags = StructuredRepositoryFacade.GetRelatedTags(this);
            }
            return this.tags;
        }
    }

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    /// <summary>
    /// Determines whether the specified Commit is equal to the current Commit.
    /// </summary>
    /// <param name="rhs">The Commit to compare with the current Commit.</param>
    /// <returns>true if the specified Commit is equal to the current Commit; otherwise, false.</returns>
    public bool Equals(Commit rhs) =>
        rhs is { } &&
        this.Hash.Equals(rhs.Hash) &&
        this.treeRoot.Equals(rhs.treeRoot) &&
        this.Author.Equals(rhs.Author) &&
        this.Committer.Equals(rhs.Committer) &&
        this.parents.SequenceEqual(rhs.parents) &&
        this.message.Equals(rhs.message);

    bool IEquatable<Commit>.Equals(Commit? rhs) =>
        this.Equals(rhs!);

    /// <summary>
    /// Determines whether the specified object is equal to the current Commit.
    /// </summary>
    /// <param name="obj">The object to compare with the current Commit.</param>
    /// <returns>true if the specified object is equal to the current Commit; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Commit rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.treeRoot.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Author.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.message.GetHashCode();
            return this.parents.Aggregate(
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
    /// Returns a string representation of the commit.
    /// </summary>
    /// <returns>A string representation of the commit.</returns>
    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Subject.Replace('\n', ' ')}";
}
