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

/// <summary>
/// Represents the status of files in the working directory.
/// </summary>
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

    /// <summary>
    /// Gets the files that are staged for commit.
    /// </summary>
    public ReadOnlyArray<WorkingDirectoryFile> StagedFiles { get; }
    
    /// <summary>
    /// Gets the files that have unstaged changes.
    /// </summary>
    public ReadOnlyArray<WorkingDirectoryFile> UnstagedFiles { get; }
    
    /// <summary>
    /// Gets the files that are not tracked by Git.
    /// </summary>
    public ReadOnlyArray<WorkingDirectoryFile> UntrackedFiles { get; }

    /// <summary>
    /// Returns a string representation of the working directory status.
    /// </summary>
    /// <returns>A string representation of the working directory status.</returns>
    public override string ToString() =>
        $"Staged: {this.StagedFiles.Count}, Unstaged: {this.UnstagedFiles.Count}, Untracked: {this.UntrackedFiles.Count}";
}
