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

public readonly struct PrimitiveWorkingDirectoryFile : IEquatable<PrimitiveWorkingDirectoryFile>
{
    public readonly string Path;
    public readonly FileStatus Status;
    public readonly Hash? IndexHash;
    public readonly Hash? WorkingTreeHash;

    public PrimitiveWorkingDirectoryFile(
        string path,
        FileStatus status,
        Hash? indexHash,
        Hash? workingTreeHash)
    {
        this.Path = path;
        this.Status = status;
        this.IndexHash = indexHash;
        this.WorkingTreeHash = workingTreeHash;
    }

    public bool Equals(PrimitiveWorkingDirectoryFile rhs) =>
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status &&
        this.IndexHash.Equals(rhs.IndexHash) &&
        this.WorkingTreeHash.Equals(rhs.WorkingTreeHash);

    bool IEquatable<PrimitiveWorkingDirectoryFile>.Equals(PrimitiveWorkingDirectoryFile rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveWorkingDirectoryFile rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Path.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Status.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IndexHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.WorkingTreeHash.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Status}: {this.Path}";
} 