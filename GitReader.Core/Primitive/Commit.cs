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

public readonly struct Commit : IEquatable<Commit>
{
    public readonly Hash Hash;
    public readonly Hash TreeRoot;
    public readonly Signature Author;
    public readonly Signature Committer;
    public readonly Hash[] Parents;
    public readonly string Message;

    private Commit(
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

    public bool Equals(Commit rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.TreeRoot.Equals(rhs.TreeRoot) &&
        this.Author.Equals(rhs.Author) &&
        this.Committer.Equals(rhs.Committer) &&
        this.Parents.SequenceEqual(rhs.Parents) &&
        this.Message.Equals(rhs.Message);

    bool IEquatable<Commit>.Equals(Commit rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Commit rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Parents.Aggregate(
            this.Hash.GetHashCode() ^
            this.TreeRoot.GetHashCode() ^
            this.Author.GetHashCode() ^
            this.Committer.GetHashCode() ^
            this.Message.GetHashCode(),
            (agg, v) => agg ^ v.GetHashCode());

    public override string ToString() =>
        $"{this.Hash}: {this.Author}: {this.Message.Replace('\n', ' ')}";

    public static implicit operator Hash(Commit commit) =>
        commit.Hash;

    public static Commit Create(
        Hash hash,
        Hash treeRoot,
        Signature author,
        Signature committer,
        Hash[] parents,
        string message) =>
        new(hash, treeRoot, author, committer, parents, message);
}
