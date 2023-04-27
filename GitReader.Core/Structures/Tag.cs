////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Structures;

public abstract class Tag : IEquatable<Tag?>
{
    public readonly Hash Hash;
    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    private protected Tag(
        Hash hash, string name, Signature? tagger, string? message)
    {
        this.Hash = hash;
        this.Name = name;
        this.Tagger = tagger;
        this.Message = message;
    }

    public abstract ObjectTypes Type { get; }

    public bool Equals(Tag rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Name.Equals(rhs.Name) &&
        this.Type == rhs.Type &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Tag?>.Equals(Tag? rhs) =>
        rhs is { } && this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Tag rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Hash.GetHashCode() ^
        this.Name.GetHashCode() ^
        this.Type.GetHashCode() ^
        this.Tagger.GetHashCode() ^
        (this.Message?.GetHashCode() ?? 0);

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.Hash}";
}

public sealed class CommitTag : Tag
{
    public readonly Commit Commit;

    internal CommitTag(
        Hash hash, string name, Signature? tagger, string? message,
        Commit commit) :
        base(hash, name, tagger, message) =>
        this.Commit = commit;

    public override ObjectTypes Type =>
        ObjectTypes.Commit;
}
