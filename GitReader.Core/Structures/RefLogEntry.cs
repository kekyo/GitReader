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

public sealed class RefLogEntry : IEquatable<RefLogEntry>
{
    public readonly Commit Commit;
    public readonly Commit OldCommit;
    public readonly Signature Committer;
    public readonly string Message;

    internal RefLogEntry(
        Commit commit, Commit oldCommit, Signature committer, string message)
    {
        this.Commit = commit;
        this.OldCommit = oldCommit;
        this.Committer = committer;
        this.Message = message;
    }

    public bool Equals(RefLogEntry rhs) =>
        rhs is { } &&
        (object.ReferenceEquals(this, rhs) ||
            (this.Commit.Equals(rhs.Commit) &&
             this.OldCommit.Equals(rhs.OldCommit) &&
             this.Committer.Equals(rhs.Committer) &&
             this.Message.Equals(rhs.Message)));

    bool IEquatable<RefLogEntry>.Equals(RefLogEntry? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is RefLogEntry rhs && this.Equals(rhs);

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