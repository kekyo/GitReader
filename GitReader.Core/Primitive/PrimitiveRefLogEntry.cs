using System;
using System.Diagnostics.CodeAnalysis;

namespace GitReader.Primitive;

public class PrimitiveRefLogEntry : IEquatable<PrimitiveRefLogEntry>
{
    public readonly Hash Old;
    public readonly Hash Current;
    public readonly Signature Committer;
    public readonly string Message;
    
    private PrimitiveRefLogEntry(Hash old, Hash current, Signature committer, string message)
    {
        Old = old;
        Current = current;
        Committer = committer;
        Message = message;
    }

    public static bool TryParse(string line, [NotNullWhen(true)]out PrimitiveRefLogEntry? refLogEntry)
    {
        if (string.IsNullOrEmpty(line))
        {
            refLogEntry = null;
            return false;
        }
        
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

        refLogEntry = new PrimitiveRefLogEntry(old, current, committer, message);
        return true;
    }

    public bool Equals(PrimitiveRefLogEntry? other)
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
        return Equals((PrimitiveRefLogEntry)obj);
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