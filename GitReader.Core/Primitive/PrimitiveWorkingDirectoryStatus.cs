////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using System;

namespace GitReader.Primitive;

public readonly struct PrimitiveWorkingDirectoryStatus : IEquatable<PrimitiveWorkingDirectoryStatus>
{
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> StagedFiles;
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> UnstagedFiles;
    public readonly ReadOnlyArray<PrimitiveWorkingDirectoryFile> UntrackedFiles;

    public PrimitiveWorkingDirectoryStatus(
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> stagedFiles,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> unstagedFiles,
        ReadOnlyArray<PrimitiveWorkingDirectoryFile> untrackedFiles)
    {
        this.StagedFiles = stagedFiles;
        this.UnstagedFiles = unstagedFiles;
        this.UntrackedFiles = untrackedFiles;
    }

    public bool Equals(PrimitiveWorkingDirectoryStatus rhs) =>
        this.StagedFiles.Equals(rhs.StagedFiles) &&
        this.UnstagedFiles.Equals(rhs.UnstagedFiles) &&
        this.UntrackedFiles.Equals(rhs.UntrackedFiles);

    bool IEquatable<PrimitiveWorkingDirectoryStatus>.Equals(PrimitiveWorkingDirectoryStatus rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveWorkingDirectoryStatus rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.StagedFiles.GetHashCode();
            hashCode = (hashCode * 397) ^ this.UnstagedFiles.GetHashCode();
            hashCode = (hashCode * 397) ^ this.UntrackedFiles.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"Staged: {this.StagedFiles.Count}, Unstaged: {this.UnstagedFiles.Count}, Untracked: {this.UntrackedFiles.Count}";
} 