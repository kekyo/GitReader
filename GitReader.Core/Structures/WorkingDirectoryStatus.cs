////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;

namespace GitReader.Structures;

public sealed class WorkingDirectoryStatus
{
    internal WorkingDirectoryStatus(
        ReadOnlyArray<WorkingDirectoryFile> stagedFiles,
        ReadOnlyArray<WorkingDirectoryFile> unstagedFiles,
        ReadOnlyArray<WorkingDirectoryFile> untrackedFiles)
    {
        this.StagedFiles = stagedFiles;
        this.UnstagedFiles = unstagedFiles;
        this.UntrackedFiles = untrackedFiles;
    }

    public ReadOnlyArray<WorkingDirectoryFile> StagedFiles { get; }
    public ReadOnlyArray<WorkingDirectoryFile> UnstagedFiles { get; }
    public ReadOnlyArray<WorkingDirectoryFile> UntrackedFiles { get; }

    public override string ToString() =>
        $"Staged: {this.StagedFiles.Count}, Unstaged: {this.UnstagedFiles.Count}, Untracked: {this.UntrackedFiles.Count}";
} 