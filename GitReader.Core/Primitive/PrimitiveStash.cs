using System;

namespace GitReader.Primitive;

public class PrimitiveStash : IEquatable<PrimitiveStash>
{
    public readonly Hash Hash;
    public readonly Signature Signature;
    public readonly string Message;
    
    public PrimitiveStash(Hash hash, Signature signature, string message)
    {
        Hash = hash;
        Signature = signature;
        Message = message;
    }

    public bool Equals(PrimitiveStash? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Hash.Equals(other.Hash) && Signature.Equals(other.Signature) && Message == other.Message;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PrimitiveStash)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ Signature.GetHashCode();
            hashCode = (hashCode * 397) ^ Message.GetHashCode();
            return hashCode;
        }
    }
}