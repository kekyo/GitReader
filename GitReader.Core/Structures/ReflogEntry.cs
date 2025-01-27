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

public sealed class ReflogEntry :
    IEquatable<ReflogEntry>, IRepositoryReference
{
    private readonly WeakReference rwr;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Commit;
    public readonly Hash OldCommit;

    public readonly Signature Committer;
    public readonly string Message;

    internal ReflogEntry(
        WeakReference rwr,
        Hash commit, Hash oldCommit, Signature committer,
        string message)
    {
        this.rwr = rwr;
        this.Commit = commit;
        this.OldCommit = oldCommit;
        this.Committer = committer;
        this.Message = message;
    }

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public bool Equals(ReflogEntry rhs) =>
        rhs is { } &&
        (object.ReferenceEquals(this, rhs) ||
            (this.Commit.Equals(rhs.Commit) &&
             this.OldCommit.Equals(rhs.OldCommit) &&
             this.Committer.Equals(rhs.Committer) &&
             this.Message.Equals(rhs.Message)));

    bool IEquatable<ReflogEntry>.Equals(ReflogEntry? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is ReflogEntry rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Commit.GetHashCode();
            hashCode = (hashCode * 397) ^ this.OldCommit.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return hashCode;
        }
    }
}