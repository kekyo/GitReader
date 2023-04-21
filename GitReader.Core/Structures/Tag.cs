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

public sealed class Tag : IEquatable<Tag?>
{
    public readonly Hash Hash;
    public readonly ObjectTypes Type;
    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    internal Tag(Primitive.Tag tag)
    {
        this.Hash = tag.Hash;
        this.Type = tag.Type;
        this.Name = tag.Name;
        this.Tagger = tag.Tagger;
        this.Message = tag.Message;
    }

    internal Tag(
        Hash hash, ObjectTypes type, string name)
    {
        this.Hash = hash;
        this.Type = type;
        this.Name = name;
    }

    public bool Equals(Primitive.Tag rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Type.Equals(rhs.Type) &&
        this.Name.Equals(rhs.Name) &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Tag?>.Equals(Tag? rhs) =>
        rhs is { } && this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Tag rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Hash.GetHashCode() ^
        this.Type.GetHashCode() ^
        this.Name.GetHashCode() ^
        this.Tagger.GetHashCode() ^
        (this.Message?.GetHashCode() ?? 0);

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.Hash}";

    public void Deconstruct(
        out Hash hash,
        out ObjectTypes type,
        out string name,
        out Signature? tagger,
        out string? message)
    {
        hash = this.Hash;
        type = this.Type;
        name = this.Name;
        tagger = this.Tagger;
        message = this.Message;
    }
}
