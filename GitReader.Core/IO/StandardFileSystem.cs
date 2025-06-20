﻿////////////////////////////////////////////////////////////////////////////
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
/// Provides a standard implementation of the IFileSystem interface using the local file system.
/// </summary>
public sealed class StandardFileSystem : IFileSystem
{
    private static readonly string homePath =
        Path.GetFullPath(Utilities.IsWindows ?
            $"{Environment.GetEnvironmentVariable("HOMEDRIVE") ?? "C:"}{Environment.GetEnvironmentVariable("HOMEPATH") ?? "\\"}" :
            (Environment.GetEnvironmentVariable("HOME") ?? "/"));

    private readonly int bufferSize;

    /// <summary>
    /// Initializes a new instance of the StandardFileSystem class with the specified buffer size.
    /// </summary>
    /// <param name="bufferSize">The buffer size to use for file operations.</param>
    public StandardFileSystem(int bufferSize) =>
        this.bufferSize = bufferSize;

#if NET35
    /// <summary>
    /// Combines an array of paths into a single path.
    /// </summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public string Combine(params string[] paths) =>
        paths.Aggregate(Path.Combine);
#else
    /// <summary>
    /// Combines an array of paths into a single path.
    /// </summary>
    /// <param name="paths">The paths to combine.</param>
    /// <returns>The combined path.</returns>
    public string Combine(params string[] paths) =>
        Path.Combine(paths);
#endif

    /// <summary>
    /// Gets the directory path portion of the specified path.
    /// </summary>
    /// <param name="path">The path to get the directory from.</param>
    /// <returns>The directory path.</returns>
    public string GetDirectoryPath(string path) =>
        Path.GetDirectoryName(path) switch
        {
            // Not accurate in Windows, but a compromise...
            null => Path.DirectorySeparatorChar.ToString(),
            "" => string.Empty,
            var dp => dp,
        };

    /// <summary>
    /// Gets the absolute path for the specified path.
    /// </summary>
    /// <param name="path">The path to get the full path for.</param>
    /// <returns>The absolute path.</returns>
    public string GetFullPath(string path) =>
        Path.GetFullPath(path);

    /// <summary>
    /// Determines whether the specified path is rooted.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>true if the path is rooted; otherwise, false.</returns>
    public bool IsPathRooted(string path) =>
        Path.IsPathRooted(path);

    /// <summary>
    /// Resolves a relative path against a base path.
    /// </summary>
    /// <param name="basePath">The base path to resolve against.</param>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The resolved path.</returns>
    public string ResolveRelativePath(string basePath, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ?
            path :
            path.StartsWith("~/") ?
                Combine(homePath, path.Substring(2)) :
                Combine(basePath, path));

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns true if the file exists; otherwise, false.</returns>
    public Task<bool> IsFileExistsAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(File.Exists(path));

    /// <summary>
    /// Gets all files in the specified directory that match the given pattern.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="match">The search pattern.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of file paths.</returns>
    public Task<string[]> GetFilesAsync(
        string basePath, string match, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(basePath) ?
            Directory.GetFiles(basePath, match, SearchOption.AllDirectories) :
            Utilities.Empty<string>());

    /// <summary>
    /// Opens a file stream for reading.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="isSeekable">Whether the stream should be seekable (currently unused).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a stream for reading the file.</returns>
    public async Task<Stream> OpenAsync(
        string path, bool isSeekable, CancellationToken ct)
    {
        // Many Git clients are supposed to be OK to use at the same time.
        // If we try to open a file with the FileShare.Read share flag (i.e., write-protected),
        // an error will occur when another Git client is opening (with non-read-sharable) the file.
        // Retry here as this situation is expected to take a short time to complete.
        // However, if multiple files are opened sequentially,
        // a deadlock may occur depending on the order in which they are opened.
        // Because they are not processed as transactions.
        // If a constraint is imposed by the number of open attempts,
        // and if the file cannot be opened by any means,
        // degrade to FileShare.ReadWrite and attempt to open it.
        // (In this case it might read the wrong, that is the value in the process of writing...)

        Random? r = null;

        for (var count = 0; count < 20; count++)
        {
            try
            {
                return new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    this.bufferSize, true);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (IOException)
            {
            }

            if (r == null)
            {
                r = new Random();
            }

            await Utilities.Delay(TimeSpan.FromMilliseconds(r.Next(10, 500)), ct);
        }

        // Gave up and will try to open with read-write...
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            65536,
            true);
    }

    /// <summary>
    /// Creates a temporary file and returns a descriptor for it.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a temporary file descriptor.</returns>
    public Task<TemporaryFileDescriptor> CreateTemporaryAsync(
        CancellationToken ct)
    {
        var path = this.Combine(
            Path.GetTempPath(),
            Path.GetTempFileName());

        var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            this.bufferSize,
            true);

        return Utilities.FromResult(new TemporaryFileDescriptor(path, stream));
    }

    /// <summary>
    /// Gets the file name portion of the specified path.
    /// </summary>
    /// <param name="path">The path to get the file name from.</param>
    /// <returns>The file name.</returns>
    public string GetFileName(string path) =>
        Path.GetFileName(path);

    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns true if the directory exists; otherwise, false.</returns>
    public Task<bool> IsDirectoryExistsAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(path));

    /// <summary>
    /// Gets all entries (files and directories) in the specified directory.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an array of entry paths.</returns>
    public Task<string[]> GetDirectoryEntriesAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(path) ?
            Directory.GetFileSystemEntries(path) :
            Utilities.Empty<string>());

    /// <summary>
    /// Gets the relative path from the base path to the target path.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="path">The target path.</param>
    /// <returns>The relative path from base to target.</returns>
    public string GetRelativePath(string basePath, string path)
    {
        if (!Path.IsPathRooted(path))
        {
            return path;
        }

        var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
        var targetUri = new Uri(Path.GetFullPath(path));
        
        var relativeUri = baseUri.MakeRelativeUri(targetUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
        
        // Convert forward slashes to platform-specific directory separators
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Converts a path to POSIX format (using forward slashes).
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>The path in POSIX format.</returns>
    public string ToPosixPath(string path) =>
        Utilities.IsWindows ? path.Replace('\\', '/') : path;
}
