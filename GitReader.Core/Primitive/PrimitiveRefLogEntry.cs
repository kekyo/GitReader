////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

public readonly struct PrimitiveRefLogEntry :
    IEquatable<PrimitiveRefLogEntry>
{
    public readonly Hash Old;
    public readonly Hash Current;
    public readonly Signature Committer;
    public readonly string Message;
    
    private PrimitiveRefLogEntry(
        Hash old, Hash current, Signature committer, string message)
    {
        this.Old = old;
        this.Current = current;
        this.Committer = committer;
        this.Message = message;
    }

    public bool Equals(PrimitiveRefLogEntry other) =>
        this.Old.Equals(other.Old) &&
        this.Current.Equals(other.Current) &&
        this.Committer.Equals(other.Committer) &&
        this.Message == other.Message;

    bool IEquatable<PrimitiveRefLogEntry>.Equals(PrimitiveRefLogEntry rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveRefLogEntry rhs && this.Equals(rhs);

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

    public static bool TryParse(
        string line, out PrimitiveRefLogEntry refLogEntry)
    {
        if (string.IsNullOrEmpty(line))
        {
            refLogEntry = default;
            return false;
        }

        var columns = line.Split('\t');
        if (columns.Length < 2)
        {
            refLogEntry = default;
            return false;
        }

        const int hashLength = 40;

        if (!Hash.TryParse(columns[0].Substring(0, hashLength), out var old))
        {
            refLogEntry = default;
            return false;
        }

        if (!Hash.TryParse(columns[0].Substring(hashLength + 1, hashLength), out var current))
        {
            refLogEntry = default;
            return false;
        }

        if (!Signature.TryParse(columns[0].Substring((hashLength + 1) * 2), out var committer))
        {
            refLogEntry = default;
            return false;
        }

        var message = columns[1].Trim();

        refLogEntry = new(old, current, committer, message);
        return true;
    }
}