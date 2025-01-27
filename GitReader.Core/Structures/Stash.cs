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

public sealed class Stash :
    IEquatable<Stash>, IInternalCommitReference
{
    private readonly WeakReference rwr;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Hash;

    public readonly Signature Committer;
    public readonly string Message;

    internal Stash(
        WeakReference rwr, Hash hash, Signature committer, string message)
    {
        this.rwr = rwr;
        this.Hash = hash;
        this.Committer = committer;
        this.Message = message;
    }

    Hash ICommitReference.Hash =>
        this.Hash;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public bool Equals(Stash rhs) =>
        rhs is { } &&
        (object.ReferenceEquals(this, rhs) ||
            (this.Hash.Equals(rhs.Hash) &&
             this.Committer.Equals(rhs.Committer) &&
             this.Message.Equals(rhs.Message)));

    bool IEquatable<Stash>.Equals(Stash? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Stash rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return hashCode;
        }
    }
}