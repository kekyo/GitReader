////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

public readonly struct Tag : IEquatable<Tag>
{
    public readonly Hash Hash;
    public readonly ObjectTypes Type;
    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    private Tag(
        Hash hash,
        ObjectTypes type,
        string name,
        Signature? tagger,
        string? message)
    {
        this.Hash = hash;
        this.Type = type;
        this.Name = name;
        this.Tagger = tagger;
        this.Message = message;
    }

    public bool Equals(Tag rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Type.Equals(rhs.Type) &&
        this.Name.Equals(rhs.Name) &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Tag>.Equals(Tag rhs) =>
        this.Equals(rhs);

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

    public static implicit operator Hash(Tag tag) =>
        tag.Hash;

    public static Tag Create(
        Hash hash,
        ObjectTypes type,
        string name,
        Signature? tagger,
        string? message) =>
        new(hash, type, name, tagger, message);
}
