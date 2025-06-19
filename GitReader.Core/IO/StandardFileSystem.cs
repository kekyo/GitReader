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

public sealed class StandardFileSystem : IFileSystem
{
    private static readonly string homePath =
        Path.GetFullPath(Utilities.IsWindows ?
            $"{Environment.GetEnvironmentVariable("HOMEDRIVE") ?? "C:"}{Environment.GetEnvironmentVariable("HOMEPATH") ?? "\\"}" :
            (Environment.GetEnvironmentVariable("HOME") ?? "/"));

    private readonly int bufferSize;

    public StandardFileSystem(int bufferSize) =>
        this.bufferSize = bufferSize;

#if NET35
    public string Combine(params string[] paths) =>
        paths.Aggregate(Path.Combine);
#else
    public string Combine(params string[] paths) =>
        Path.Combine(paths);
#endif

    public string GetDirectoryPath(string path) =>
        Path.GetDirectoryName(path) switch
        {
            // Not accurate in Windows, but a compromise...
            null => Path.DirectorySeparatorChar.ToString(),
            "" => string.Empty,
            var dp => dp,
        };

    public string GetFullPath(string path) =>
        Path.GetFullPath(path);

    public bool IsPathRooted(string path) =>
        Path.IsPathRooted(path);

    public string ResolveRelativePath(string basePath, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ?
            path :
            path.StartsWith("~/") ?
                Combine(homePath, path.Substring(2)) :
                Combine(basePath, path));

    public Task<bool> IsFileExistsAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(File.Exists(path));

    public Task<string[]> GetFilesAsync(
        string basePath, string match, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(basePath) ?
            Directory.GetFiles(basePath, match, SearchOption.AllDirectories) :
            Utilities.Empty<string>());

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

    public string GetFileName(string path) =>
        Path.GetFileName(path);

    public Task<bool> IsDirectoryExistsAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(path));

    public Task<string[]> GetDirectoryEntriesAsync(string path, CancellationToken ct) =>
        Utilities.FromResult(Directory.Exists(path) ?
            Directory.GetFileSystemEntries(path) :
            Utilities.Empty<string>());

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

    public string ToPosixPath(string path) =>
        Utilities.IsWindows ? path.Replace('\\', '/') : path;
}
