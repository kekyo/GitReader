using System;

namespace GitReader.Structures;

public class RefLogEntry : IEquatable<RefLogEntry>
{
    public readonly Commit Commit;
    public readonly Commit OldCommit;
    public readonly Signature Committer;
    public readonly string Message;
    
    public RefLogEntry(Commit commit, Commit oldCommit, Signature committer, string message)
    {
        Commit = commit;
        OldCommit = oldCommit;
        Committer = committer;
        Message = message;
    }

    public bool Equals(RefLogEntry? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Commit.Equals(other.Commit) && OldCommit.Equals(other.OldCommit) && Committer.Equals(other.Committer) && Message == other.Message;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RefLogEntry)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Commit.GetHashCode();
            hashCode = (hashCode * 397) ^ OldCommit.GetHashCode();
            hashCode = (hashCode * 397) ^ Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ Message.GetHashCode();
            return hashCode;
        }
    }
}