////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace GitReader.Structures;

public sealed class WorkingDirectoryFile : IRepositoryReference
{
    private readonly WeakReference rwr;

    internal WorkingDirectoryFile(
        WeakReference rwr,
        string path,
        FileStatus status,
        Hash? indexHash,
        Hash? workingTreeHash)
    {
        this.rwr = rwr;
        this.Path = path;
        this.Status = status;
        this.IndexHash = indexHash;
        this.WorkingTreeHash = workingTreeHash;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public string Path { get; }
    public FileStatus Status { get; }
    public Hash? IndexHash { get; }
    public Hash? WorkingTreeHash { get; }

    public override bool Equals(object? obj) =>
        obj is WorkingDirectoryFile rhs &&
        this.Path.Equals(rhs.Path) &&
        this.Status == rhs.Status &&
        this.IndexHash.Equals(rhs.IndexHash) &&
        this.WorkingTreeHash.Equals(rhs.WorkingTreeHash);

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