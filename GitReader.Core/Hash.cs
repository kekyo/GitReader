////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace GitReader;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Hash : IEquatable<Hash>
{
    public static readonly int Size = 20;

    private long hashCode0;
    private long hashCode8;
    private int hashCode16;

    private Hash(byte[] hashCode)
    {
        fixed (void* pl = &this.hashCode0)
        {
            Marshal.Copy(hashCode, 0, (nint)pl, 20);
        }
    }

    private Hash(string hashString)
    {
        fixed (void* pl_ = &this.hashCode0)
        {
            var pl = (byte*)pl_;
            for (var index = 0; index < 20; index++)
            {
                *(pl + index) = Convert.ToByte(
                    hashString.Substring(index * 2, 2), 16);
            }
        }
    }

    public byte[] HashCode
    {
        get
        {
            var hashCode = new byte[20];
            fixed (void* p = &this.hashCode0)
            {
                Marshal.Copy((nint)p, hashCode, 0, 20);
            }
            return hashCode;
        }
    }

    public bool Equals(Hash rhs) =>
        this.hashCode0 == rhs.hashCode0 &&
        this.hashCode8 == rhs.hashCode8 &&
        this.hashCode16 == rhs.hashCode16;

    bool IEquatable<Hash>.Equals(Hash rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Hash rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.hashCode0.GetHashCode() ^
        this.hashCode8.GetHashCode() ^
        this.hashCode16;

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
        if (hashString.Length != (20 * 2))
        {
            hash = default;
            return false;
        }

        hash = new(hashString);
        return true;
    }
}
