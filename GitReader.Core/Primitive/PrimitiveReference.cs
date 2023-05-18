////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive;

public readonly struct PrimitiveReference
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

    public override string ToString() =>
        $"{this.Name}: {this.Target}";

    public static implicit operator Hash(PrimitiveReference reference) =>
        reference.Target;
}
