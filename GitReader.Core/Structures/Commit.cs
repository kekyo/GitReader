////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Linq;

namespace GitReader.Structures;

public sealed class Commit : IEquatable<Commit?>
{
    internal readonly string message;
    internal readonly WeakReference rwr;
    internal readonly Hash treeRoot;
    internal readonly Hash[] parents;

    private Branch[]? branches;
    private Branch[]? remoteBranches;
    private Tag[]? tags;

    public readonly Hash Hash;
    public readonly Signature Author;
    public readonly Signature Committer;

    internal Commit(
        WeakReference rwr,
        PrimitiveCommit commit)
    {
        this.rwr = rwr;
        this.treeRoot = commit.TreeRoot;
        this.parents = commit.Parents;

        this.Hash = commit.Hash;
        this.Author = commit.Author;
        this.Committer = commit.Committer;
        this.message = commit.Message;
    }

    public string Subject
    {
        get
        {
            var index = this.message.IndexOf("\n\n");
            return ((index >= 0) ? this.message.Substring(0, index) : this.message).Trim('\n').Replace('\n', ' ');
        }
    }

    public string Body
    {
        get
        {
            var index = this.message.IndexOf("\n\n");
            return (index >= 0) ? this.message.Substring(index + 2) : string.Empty;
        }
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
        this.message.Equals(rhs.message);

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
            this.message.GetHashCode(),
            (agg, v) => agg ^ v.GetHashCode());

    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Subject.Replace('\n', ' ')}";
}
