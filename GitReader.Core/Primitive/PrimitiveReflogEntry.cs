////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

/// <summary>
/// Represents a Git reflog entry with old and current commit hashes, committer information, and message.
/// </summary>
public readonly struct PrimitiveReflogEntry :
    IEquatable<PrimitiveReflogEntry>
{
    /// <summary>
    /// The hash of the old commit.
    /// </summary>
    public readonly Hash Old;
    
    /// <summary>
    /// The hash of the current commit.
    /// </summary>
    public readonly Hash Current;
    
    /// <summary>
    /// The signature of the committer who made this reflog entry.
    /// </summary>
    public readonly Signature Committer;
    
    /// <summary>
    /// The message describing the reflog entry.
    /// </summary>
    public readonly string Message;
    
    /// <summary>
    /// Initializes a new instance of the PrimitiveReflogEntry structure.
    /// </summary>
    /// <param name="old">The hash of the old commit.</param>
    /// <param name="current">The hash of the current commit.</param>
    /// <param name="committer">The signature of the committer.</param>
    /// <param name="message">The message describing the reflog entry.</param>
    private PrimitiveReflogEntry(
        Hash old, Hash current, Signature committer, string message)
    {
        this.Old = old;
        this.Current = current;
        this.Committer = committer;
        this.Message = message;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveReflogEntry is equal to the current instance.
    /// </summary>
    /// <param name="other">The PrimitiveReflogEntry to compare with the current instance.</param>
    /// <returns>true if the specified PrimitiveReflogEntry is equal to the current instance; otherwise, false.</returns>
    public bool Equals(PrimitiveReflogEntry other) =>
        this.Old.Equals(other.Old) &&
        this.Current.Equals(other.Current) &&
        this.Committer.Equals(other.Committer) &&
        this.Message == other.Message;

    bool IEquatable<PrimitiveReflogEntry>.Equals(PrimitiveReflogEntry rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveReflogEntry rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Old.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Current.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Committer.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Message.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Attempts to parse a reflog entry from a string line.
    /// </summary>
    /// <param name="line">The string line to parse.</param>
    /// <param name="reflogEntry">When this method returns, contains the parsed reflog entry if successful; otherwise, the default value.</param>
    /// <returns>true if the line was successfully parsed; otherwise, false.</returns>
    public static bool TryParse(
        string line, out PrimitiveReflogEntry reflogEntry)
    {
        if (string.IsNullOrEmpty(line))
        {
            reflogEntry = default;
            return false;
        }

        var columns = line.Split('\t');
        if (columns.Length < 2)
        {
            reflogEntry = default;
            return false;
        }

        const int hashLength = 40;

        if (!Hash.TryParse(columns[0].Substring(0, hashLength), out var old))
        {
            reflogEntry = default;
            return false;
        }

        if (!Hash.TryParse(columns[0].Substring(hashLength + 1, hashLength), out var current))
        {
            reflogEntry = default;
            return false;
        }

        if (!Signature.TryParse(columns[0].Substring((hashLength + 1) * 2), out var committer))
        {
            reflogEntry = default;
            return false;
        }

        var message = columns[1].Trim();

        reflogEntry = new(old, current, committer, message);
        return true;
    }
}