////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

public sealed class Branch : CommitRef, IEquatable<Branch>
{
    public readonly string Name;

    public Hash Head => CommitHash;

    internal Branch(WeakReference rwr,
        string name,
        Hash head) : base(rwr, head)
    {
        this.Name = name;
    }

    public bool Equals(Branch rhs) =>
        rhs is { } &&
        this.Name.Equals(rhs.Name) &&
        this.Head.Equals(rhs.Head);

    bool IEquatable<Branch>.Equals(Branch? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Branch rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Head.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Head}: {this.Name}";
}
