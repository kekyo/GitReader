////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

/// <summary>
/// Represents a temporary file descriptor containing both the file path and its stream.
/// </summary>
public readonly struct TemporaryFileDescriptor
{
    /// <summary>
    /// The path to the temporary file.
    /// </summary>
    public readonly string Path;
    
    /// <summary>
    /// The stream for accessing the temporary file.
    /// </summary>
    public readonly Stream Stream;

    /// <summary>
    /// Initializes a new instance of the TemporaryFileDescriptor struct.
    /// </summary>
    /// <param name="path">The path to the temporary file.</param>
    /// <param name="stream">The stream for accessing the temporary file.</param>
    public TemporaryFileDescriptor(string path, Stream stream)
    {
        this.Path = path;
        this.Stream = stream;
    }

    /// <summary>
    /// Deconstructs the TemporaryFileDescriptor into its path and stream components.
    /// </summary>
    /// <param name="path">The path to the temporary file.</param>
    /// <param name="stream">The stream for accessing the temporary file.</param>
    public void Deconstruct(out string path, out Stream stream)
    {
        path = this.Path;
        stream = this.Stream;
    }
}

/// <summary>
/// Defines a contract for file system operations used by GitReader.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Combines multiple path components into a single path.
    /// </summary>
    /// <param name="paths">The path components to combine.</param>
    /// <returns>A combined path.</returns>
    string Combine(params string[] paths);

    /// <summary>
    /// Gets the directory name of the specified path.
    /// </summary>
    /// <param name="path">The path to get the directory for.</param>
    /// <returns>The directory path.</returns>
    string GetDirectoryPath(string path);

    /// <summary>
    /// Gets the absolute path for the specified path.
    /// </summary>
    /// <param name="path">The path to get the full path for.</param>
    /// <returns>The absolute path.</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Determines whether the specified path is rooted.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the path is rooted; otherwise, false.</returns>
    bool IsPathRooted(string path);

    /// <summary>
    /// Resolves a relative path based on a base path.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="path">The relative path to resolve.</param>
    /// <returns>The resolved path.</returns>
    string ResolveRelativePath(string basePath, string path);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to the file to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the file exists; otherwise, false.</returns>
    Task<bool> IsFileExistsAsync(
        string path, CancellationToken ct);

    /// <summary>
    /// Gets the files in the specified directory that match the specified pattern.
    /// </summary>
    /// <param name="basePath">The directory to search.</param>
    /// <param name="match">The search pattern to match against files.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of file paths.</returns>
    Task<string[]> GetFilesAsync(
        string basePath, string match, CancellationToken ct);

    /// <summary>
    /// Opens a file stream for the specified path.
    /// </summary>
    /// <param name="path">The path to the file to open.</param>
    /// <param name="isSeekable">true if the stream should be seekable; otherwise, false.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a stream for the file.</returns>
    Task<Stream> OpenAsync(
        string path, bool isSeekable, CancellationToken ct);

    /// <summary>
    /// Creates a temporary file and returns a descriptor for it.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a temporary file descriptor.</returns>
    Task<TemporaryFileDescriptor> CreateTemporaryAsync(
        CancellationToken ct);

    /// <summary>
    /// Gets the file name from the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name from.</param>
    /// <returns>The file name.</returns>
    string GetFileName(string path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The path to the directory to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the directory exists; otherwise, false.</returns>
    Task<bool> IsDirectoryExistsAsync(
        string path, CancellationToken ct);

    /// <summary>
    /// Gets the entries (files and directories) in the specified directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of entry paths.</returns>
    Task<string[]> GetDirectoryEntriesAsync(
        string path, CancellationToken ct);

    /// <summary>
    /// Gets the relative path from a base path to a target path.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="path">The target path.</param>
    /// <returns>The relative path.</returns>
    string GetRelativePath(string basePath, string path);

    /// <summary>
    /// Converts a path to POSIX format (using forward slashes).
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The path in POSIX format.</returns>
    string ToPosixPath(string path);
}
