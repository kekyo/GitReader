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

    public Hash(byte[] hashCode)
    {
        if (hashCode.Length < 20)
        {
            throw new ArgumentException("Invalid hash size.");
        }

        fixed (void* pl = &this.hashCode0)
        {
            Marshal.Copy(hashCode, 0, (nint)pl, 20);
        }
    }

    public Hash(byte[] hashCode, int offset)
    {
        if ((hashCode.Length - offset) < 20)
        {
            throw new ArgumentException("Invalid hash size.");
        }

        fixed (void* pl = &this.hashCode0)
        {
            Marshal.Copy(hashCode, offset, (nint)pl, 20);
        }
    }

    public Hash(string hashString)
    {
        fixed (void* pl = &this.hashCode0)
        {
            if (!TryParse(hashString, (byte*)pl))
            {
                throw new ArgumentException("Invalid hash string.");
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

    public static implicit operator Hash(byte[] hashCode) =>
        new(hashCode);
    public static implicit operator Hash(string hashString) =>
        new(hashString);

    private static bool TryParse(string hashString, byte* pl)
    {
        if (hashString.Length != (20 * 2))
        {
            return false;
        }

        static bool TryParseHexNumber(char ch, out byte value)
        {
            if (ch >= '0' && ch <= '9')
            {
                value = (byte)(ch - '0');
                return true;
            }
            else if(ch >= 'a' && ch <= 'f')
            {
                value = (byte)(ch - 'a' + 10);
                return true;
            }
            else if(ch >= 'A' && ch <= 'F')
            {
                value = (byte)(ch - 'A' + 10);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        for (var index = 0; index < 40; index += 2)
        {
            var ch1 = hashString[index];
            if (!TryParseHexNumber(ch1, out var v1))
            {
                return false;
            }

            var ch0 = hashString[index + 1];
            if (!TryParseHexNumber(ch0, out var v0))
            {
                return false;
            }

            *pl = (byte)(v1 << 4 | v0);
            pl++;
        }

        return true;
    }

    public static bool TryParse(string hashString, out Hash hash)
    {
        hash = default;

        fixed (void* pl = &hash.hashCode0)
        {
            return TryParse(hashString, (byte*)pl);
        }
    }

    public static Hash Parse(string hashString) =>
        TryParse(hashString, out var hash) ?
            hash : throw new ArgumentException("Invalid hash string.");
}
