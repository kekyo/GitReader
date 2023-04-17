////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
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
        Hash = hash;
        TreeRoot = treeRoot;
        Author = author;
        Committer = committer;
        Parents = parents;
        Message = message;
    }

    public bool Equals(Commit rhs) =>
        Hash.Equals(rhs.Hash) &&
        TreeRoot.Equals(rhs.TreeRoot) &&
        Author.Equals(rhs.Author) &&
        Committer.Equals(rhs.Committer) &&
        Parents.SequenceEqual(rhs.Parents) &&
        Message.Equals(rhs.Message);

    bool IEquatable<Commit>.Equals(Commit rhs) =>
        Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Commit rhs && Equals(rhs);

    public override int GetHashCode() =>
        Parents.Aggregate(
            Hash.GetHashCode() ^
            TreeRoot.GetHashCode() ^
            Author.GetHashCode() ^
            Committer.GetHashCode() ^
            Message.GetHashCode(),
            (agg, v) => agg ^ v.GetHashCode());

    public override string ToString() =>
        $"{Hash}: {Author}: {Message}";

    public void Deconstruct(
        out Hash hash,
        out Hash treeRoot,
        out Signature author,
        out Signature committer,
        out Hash[] parents,
        out string message)
    {
        hash = Hash;
        treeRoot = TreeRoot;
        author = Author;
        committer = Committer;
        parents = Parents;
        message = Message;
    }

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
