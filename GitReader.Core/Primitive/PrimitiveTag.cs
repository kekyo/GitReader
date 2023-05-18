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

public readonly struct PrimitiveTag : IEquatable<PrimitiveTag>
{
    public readonly Hash Hash;
    public readonly ObjectTypes Type;
    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    public PrimitiveTag(
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

    public bool Equals(PrimitiveTag rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Type.Equals(rhs.Type) &&
        this.Name.Equals(rhs.Name) &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<PrimitiveTag>.Equals(PrimitiveTag rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveTag rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.Hash}";

    public static implicit operator Hash(PrimitiveTag tag) =>
        tag.Hash;
}
