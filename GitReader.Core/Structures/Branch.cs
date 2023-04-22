////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures;

public sealed class Branch
{
    public readonly string Name;
    public readonly Commit Head;

    internal Branch(
        string name,
        Commit head)
    {
        this.Name = name;
        this.Head = head;
    }

    public override string ToString() =>
        $"{this.Head.Hash}: {this.Name}";

    public void Deconstruct(
        out string name,
        out Commit head)
    {
        name = this.Name;
        head = this.Head;
    }
}
