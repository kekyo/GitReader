////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

public readonly struct PrimitiveReference : IEquatable<PrimitiveReference>
{
    public readonly string Name;
    public readonly string RelativePath;
    public readonly Hash Target;

    public PrimitiveReference(
        string name,
        string relativePath,
        Hash target)
    {
        this.Name = name;
        this.RelativePath = relativePath;
        this.Target = target;
    }

    public bool Equals(PrimitiveReference rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.RelativePath.Equals(rhs.RelativePath) &&
        this.Target.Equals(rhs.Target);

    bool IEquatable<PrimitiveReference>.Equals(PrimitiveReference rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveReference rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.RelativePath.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Target.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.Target}";

    public static implicit operator Hash(PrimitiveReference reference) =>
        reference.Target;
}
