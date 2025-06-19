////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GitReader;

/// <summary>
/// Represents a Git object hash value (SHA-1).
/// </summary>
[DebuggerDisplay("{PrettyPrint}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Hash : IEquatable<Hash>
{
    /// <summary>
    /// The size of the hash in bytes (20 bytes for SHA-1).
    /// </summary>
    public static readonly int Size = 20;

    private long hashCode0;
    private long hashCode8;
    private int hashCode16;

    /// <summary>
    /// Initializes a new instance of the Hash struct from a byte array.
    /// </summary>
    /// <param name="hashCode">The byte array containing the hash value. Must be at least 20 bytes long.</param>
    /// <exception cref="ArgumentException">Thrown when the hash array is less than 20 bytes.</exception>
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

    /// <summary>
    /// Initializes a new instance of the Hash struct from a byte array at a specified offset.
    /// </summary>
    /// <param name="hashCode">The byte array containing the hash value.</param>
    /// <param name="offset">The offset in the array where the hash begins.</param>
    /// <exception cref="ArgumentException">Thrown when the remaining array length from offset is less than 20 bytes.</exception>
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

    /// <summary>
    /// Initializes a new instance of the Hash struct from a hexadecimal string representation.
    /// </summary>
    /// <param name="hashString">The hexadecimal string representation of the hash (40 characters).</param>
    /// <exception cref="ArgumentException">Thrown when the hash string is invalid.</exception>
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

    /// <summary>
    /// Gets the hash value as a byte array.
    /// </summary>
    public readonly byte[] HashCode
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

    private string PrettyPrint =>
        this.ToString();

    /// <summary>
    /// Determines whether the specified Hash is equal to the current Hash.
    /// </summary>
    /// <param name="rhs">The Hash to compare with the current Hash.</param>
    /// <returns>true if the specified Hash is equal to the current Hash; otherwise, false.</returns>
    public readonly bool Equals(Hash rhs) =>
        this.hashCode0 == rhs.hashCode0 &&
        this.hashCode8 == rhs.hashCode8 &&
        this.hashCode16 == rhs.hashCode16;

    readonly bool IEquatable<Hash>.Equals(Hash rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current Hash.
    /// </summary>
    /// <param name="obj">The object to compare with the current Hash.</param>
    /// <returns>true if the specified object is equal to the current Hash; otherwise, false.</returns>
    public readonly override bool Equals(object? obj) =>
        obj is Hash rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public readonly override int GetHashCode()
    {
        fixed (void* p = &this.hashCode0)
        {
            int* pi = (int*)p;
            var sum = *pi++;    // sizeof(int) * 5 = 20
            sum ^= *pi++;
            sum ^= *pi++;
            sum ^= *pi++;
            sum ^= *pi++;
            return sum;
        }
    }

    /// <summary>
    /// Returns a string representation of the hash in lowercase hexadecimal format.
    /// </summary>
    /// <returns>A lowercase hexadecimal string representation of the hash.</returns>
    public readonly override string ToString() =>
        BitConverter.ToString(this.HashCode).
        Replace("-", string.Empty).
        ToLowerInvariant();

    /// <summary>
    /// Implicitly converts a byte array to a Hash.
    /// </summary>
    /// <param name="hashCode">The byte array to convert.</param>
    /// <returns>A Hash instance created from the byte array.</returns>
    public static implicit operator Hash(byte[] hashCode) =>
        new(hashCode);
    
    /// <summary>
    /// Implicitly converts a hexadecimal string to a Hash.
    /// </summary>
    /// <param name="hashString">The hexadecimal string to convert.</param>
    /// <returns>A Hash instance created from the string.</returns>
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

    /// <summary>
    /// Tries to parse a hexadecimal string representation to a Hash.
    /// </summary>
    /// <param name="hashString">The hexadecimal string to parse.</param>
    /// <param name="hash">When this method returns, contains the parsed Hash if the conversion succeeded, or a default Hash if the conversion failed.</param>
    /// <returns>true if the conversion succeeded; otherwise, false.</returns>
    public static bool TryParse(string hashString, out Hash hash)
    {
        hash = default;

        fixed (void* pl = &hash.hashCode0)
        {
            return TryParse(hashString, (byte*)pl);
        }
    }

    /// <summary>
    /// Parses a hexadecimal string representation to a Hash.
    /// </summary>
    /// <param name="hashString">The hexadecimal string to parse.</param>
    /// <returns>A Hash instance parsed from the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the hash string is invalid.</exception>
    public static Hash Parse(string hashString) =>
        TryParse(hashString, out var hash) ?
            hash : throw new ArgumentException("Invalid hash string.");
}
