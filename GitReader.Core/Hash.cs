////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Security.Cryptography;

namespace GitReader;

public readonly struct Hash : IEquatable<Hash>
{
    public static readonly int Size;

    static Hash()
    {
        using var sha1 = SHA1.Create();
        Size = sha1.HashSize / 8;
    }

    public readonly byte[] HashCode;

    private Hash(byte[] hashCode) =>
        this.HashCode = hashCode;

    public bool Equals(Hash rhs)
    {
        if (rhs.HashCode == null)
        {
            return false;
        }

        for (var index = 0; index < this.HashCode.Length; index++)
        {
            if (this.HashCode[index] != rhs.HashCode[index])
            {
                return false;
            }
        }
        return true;
    }

    bool IEquatable<Hash>.Equals(Hash rhs) =>
        Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Hash rhs && Equals(rhs);

    public override int GetHashCode()
    {
        var sum = 0;
        var index = 0;
        while (index < (this.HashCode?.Length ?? 0))
        {
            sum ^= BitConverter.ToInt32(this.HashCode!, index);
            index += 4;
        }
        return sum;
    }

    public override string ToString() =>
        BitConverter.ToString(this.HashCode).
        Replace("-", string.Empty).
        ToLowerInvariant();

    public void Deconstruct(out byte[] hashCode) =>
        hashCode = this.HashCode;

    public void Deconstruct(out string hashString) =>
        hashString = this.ToString();

    public static implicit operator Hash(byte[] hashCode) =>
        Create(hashCode);
    public static implicit operator Hash(string hashString) =>
        Parse(hashString);

    public static Hash Create(byte[] hashCode)
    {
        if (hashCode.Length != Size)
        {
            throw new ArgumentException("Invalid hash size.");
        }

        return new(hashCode);
    }

    public static Hash Parse(string hashString) =>
        TryParse(hashString, out var hash) ?
            hash : throw new ArgumentException(nameof(hashString));

    public static bool TryParse(string hashString, out Hash hash)
    {
        if (hashString.Length != Size * 2)
        {
            hash = default;
            return false;
        }

        var hashCode = new byte[Size];
        for (var index = 0; index < hashCode.Length; index++)
        {
            hashCode[index] = Convert.ToByte(
                hashString.Substring(index * 2, 2), 16);
        }

        hash = new(hashCode);
        return true;
    }
}
