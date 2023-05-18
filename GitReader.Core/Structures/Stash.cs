////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Structures;

public sealed class Stash : IEquatable<Stash>
{
    public readonly Commit Commit;
    public readonly Signature Committer;
    public readonly string Message;

    internal Stash(
        Commit commit, Signature committer, string message)
    {
        this.Commit = commit;
        this.Committer = committer;
        this.Message = message;
    }

    public bool Equals(Stash rhs) =>
        rhs is { } &&
        (object.ReferenceEquals(this, rhs) ||
            (this.Commit.Equals(rhs.Commit) &&
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
            var hashCode = this.Commit.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return hashCode;
        }
    }
}