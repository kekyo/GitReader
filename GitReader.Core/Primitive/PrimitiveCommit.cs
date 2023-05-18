////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

namespace GitReader.Primitive;

public readonly struct PrimitiveCommit : IEquatable<PrimitiveCommit>
{
    public readonly Hash Hash;
    public readonly Hash TreeRoot;
    public readonly Signature Author;
    public readonly Signature Committer;
    public readonly Hash[] Parents;
    public readonly string Message;

    public PrimitiveCommit(
        Hash hash,
        Hash treeRoot,
        Signature author,
        Signature committer,
        Hash[] parents,
        string message)
    {
        this.Hash = hash;
        this.TreeRoot = treeRoot;
        this.Author = author;
        this.Committer = committer;
        this.Parents = parents;
        this.Message = message;
    }

    public bool Equals(PrimitiveCommit rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.TreeRoot.Equals(rhs.TreeRoot) &&
        this.Author.Equals(rhs.Author) &&
        this.Committer.Equals(rhs.Committer) &&
        this.Parents.SequenceEqual(rhs.Parents) &&
        this.Message.Equals(rhs.Message);

    bool IEquatable<PrimitiveCommit>.Equals(PrimitiveCommit rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveCommit rhs && this.Equals(rhs);

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

    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Message.Replace('\n', ' ')}";

    public static implicit operator Hash(PrimitiveCommit commit) =>
        commit.Hash;
}
