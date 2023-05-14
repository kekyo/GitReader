using System;
using System.Diagnostics.CodeAnalysis;

namespace GitReader.Primitive;

public class RefLogEntry : IEquatable<RefLogEntry>
{
    public readonly Hash Old;
    public readonly Hash Current;
    public readonly Signature Committer;
    public readonly string Message;
    
    private RefLogEntry(Hash old, Hash current, Signature committer, string message)
    {
        Old = old;
        Current = current;
        Committer = committer;
        Message = message;
    }

    public static bool TryParse(string line, [NotNullWhen(true)]out RefLogEntry? refLogEntry)
    {
        var columns = line.Split('\t');
        if (columns.Length < 2)
        {
            refLogEntry = null;
            return false;
        }

        const int hashLength = 40;
        
        if (!Hash.TryParse(columns[0].Substring(0, hashLength), out var old))
        {
            refLogEntry = null;
            return false;
        }

        if (!Hash.TryParse(columns[0].Substring(hashLength + 1, hashLength), out var current))
        {
            refLogEntry = null;
            return false;
        }
            
        if (!Signature.TryParse(columns[0].Substring((hashLength + 1) * 2), out var committer))
        {
            refLogEntry = null;
            return false;
        }

        var message = columns[1].Trim();

        refLogEntry = new RefLogEntry(old, current, committer, message);
        return true;
    }

    public bool Equals(RefLogEntry? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Old.Equals(other.Old) && Current.Equals(other.Current) && Committer.Equals(other.Committer) && Message == other.Message;
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
            var hashCode = Old.GetHashCode();
            hashCode = (hashCode * 397) ^ Current.GetHashCode();
            hashCode = (hashCode * 397) ^ Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ Message.GetHashCode();
            return hashCode;
        }
    }
}