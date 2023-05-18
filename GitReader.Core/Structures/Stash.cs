using System;

namespace GitReader.Structures;

public class Stash : IEquatable<Stash>
{
    public readonly Commit Commit;
    public readonly Signature Committer;
    public readonly string Message;

    public Stash(Commit commit, Signature committer, string message)
    {
        Commit = commit;
        Committer = committer;
        Message = message;
    }

    public bool Equals(Stash? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Commit.Equals(other.Commit) && Committer.Equals(other.Committer) && Message == other.Message;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Stash)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Commit.GetHashCode();
            hashCode = (hashCode * 397) ^ Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ Message.GetHashCode();
            return hashCode;
        }
    }
}