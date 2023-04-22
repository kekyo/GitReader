////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.Linq;

namespace GitReader.Structures;

public sealed class Commit : IEquatable<Commit?>
{
    internal readonly WeakReference rwr;
    internal readonly Hash treeRoot;
    internal readonly Hash[] parents;

    private Branch[]? branches;
    private Branch[]? remoteBranches;
    private Tag[]? tags;

    public readonly Hash Hash;
    public readonly Signature Author;
    public readonly Signature Committer;
    public readonly string Message;

    internal Commit(
        WeakReference rwr,
        Primitive.Commit commit)
    {
        this.rwr = rwr;
        this.treeRoot = commit.TreeRoot;
        this.parents = commit.Parents;

        this.Hash = commit.Hash;
        this.Author = commit.Author;
        this.Committer = commit.Committer;
        this.Message = commit.Message;
    }

    public Branch[] Branches
    {
        get
        {
            if (this.branches == null)
            {
                // Beginning of race condition section,
                // but will discard dict later silently.
                this.branches = RepositoryFacade.GetRelatedBranches(this);
            }
            return this.branches;
        }
    }

    public Branch[] RemoteBranches
    {
        get
        {
            if (this.remoteBranches == null)
            {
                // Beginning of race condition section,
                // but will discard dict later silently.
                this.remoteBranches = RepositoryFacade.GetRelatedRemoteBranches(this);
            }
            return this.remoteBranches;
        }
    }

    public Tag[] Tags
    {
        get
        {
            if (this.tags == null)
            {
                // Beginning of race condition section,
                // but will discard dict later silently.
                this.tags = RepositoryFacade.GetRelatedTags(this);
            }
            return this.tags;
        }
    }

    public bool Equals(Commit rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.treeRoot.Equals(rhs.treeRoot) &&
        this.Author.Equals(rhs.Author) &&
        this.Committer.Equals(rhs.Committer) &&
        this.parents.SequenceEqual(rhs.parents) &&
        this.Message.Equals(rhs.Message);

    bool IEquatable<Commit?>.Equals(Commit? rhs) =>
        rhs is { } && this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Commit rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.parents.Aggregate(
            this.Hash.GetHashCode() ^
            this.treeRoot.GetHashCode() ^
            this.Author.GetHashCode() ^
            this.Committer.GetHashCode() ^
            this.Message.GetHashCode(),
            (agg, v) => agg ^ v.GetHashCode());

    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Message}";

    public void Deconstruct(
        out Hash hash,
        out Signature author,
        out Signature committer,
        out string message)
    {
        hash = this.Hash;
        author = this.Author;
        committer = this.Committer;
        message = this.Message;
    }
}
