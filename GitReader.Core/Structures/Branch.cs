////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace GitReader.Structures;

public sealed class Branch :
    IEquatable<Branch>, IInternalCommitReference
{
    private readonly WeakReference rwr;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Head;

    public readonly string Name;
    public readonly bool IsRemote;

    internal Branch(
        WeakReference rwr,
        string name,
        Hash head,
        bool isRemote)
    {
        this.rwr = rwr;
        this.Head = head;
        this.Name = name;
        this.IsRemote = isRemote;
    }

    Hash ICommitReference.Hash =>
        this.Head;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public bool Equals(Branch rhs) =>
        rhs is { } &&
        this.Name.Equals(rhs.Name) &&
        this.Head.Equals(rhs.Head) &&
        this.IsRemote == rhs.IsRemote;

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
            hashCode = (hashCode * 397) ^ this.IsRemote.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Head}: {this.Name}{(this.IsRemote ? " [remote]" : "")}";
}
