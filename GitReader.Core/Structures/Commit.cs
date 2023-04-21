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

namespace GitReader.Structures;

public sealed class Commit : IEquatable<Commit?>
{
    internal readonly Repository repository;
    internal readonly Hash treeRoot;
    internal readonly Hash[] parents;

    public readonly Hash Hash;
    public readonly Signature Author;
    public readonly Signature Committer;
    public readonly string Message;

    internal Commit(
        Repository repository,
        Primitive.Commit commit)
    {
        this.repository = repository;
        this.treeRoot = commit.TreeRoot;
        this.parents = commit.Parents;

        this.Hash = commit.Hash;
        this.Author = commit.Author;
        this.Committer = commit.Committer;
        this.Message = commit.Message;
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
