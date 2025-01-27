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

public readonly struct PrimitiveTagReference :
    IEquatable<PrimitiveTagReference>
{
    public readonly string Name;
    public readonly string RelativePath;
    public readonly Hash ObjectOrCommitHash;
    public readonly Hash? CommitHash;

    public PrimitiveTagReference(
        string name,
        string relativePath,
        Hash objectOrCommitHash,
        Hash? commitHash)
    {
        this.Name = name;
        this.RelativePath = relativePath;
        this.ObjectOrCommitHash = objectOrCommitHash;
        this.CommitHash = commitHash;
    }

    public bool Equals(PrimitiveTagReference rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.RelativePath.Equals(rhs.RelativePath) &&
        this.ObjectOrCommitHash.Equals(rhs.ObjectOrCommitHash) &&
        this.CommitHash.Equals(rhs.CommitHash);

    bool IEquatable<PrimitiveTagReference>.Equals(PrimitiveTagReference rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveTagReference rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.RelativePath.GetHashCode();
            hashCode = (hashCode * 397) ^ this.ObjectOrCommitHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.CommitHash.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.ObjectOrCommitHash}{(this.CommitHash is { } ct ? $" [{ct}]" : "")}";
}
