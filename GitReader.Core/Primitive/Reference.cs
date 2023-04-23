////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive;

public readonly struct Reference
{
    public readonly string Name;
    public readonly Hash Target;

    private Reference(
        string name,
        Hash target)
    {
        this.Name = name;
        this.Target = target;
    }

    public override string ToString() =>
        $"{this.Name}: {this.Target}";

    public static implicit operator Hash(Reference reference) =>
        reference.Target;

    public static Reference Create(
        string name,
        Hash target) =>
        new(name, target);
}
