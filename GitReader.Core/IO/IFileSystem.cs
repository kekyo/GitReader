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

public readonly struct TemporaryFileDescriptor
{
    public readonly string Path;
    public readonly Stream Stream;

    public TemporaryFileDescriptor(string path, Stream stream)
    {
        this.Path = path;
        this.Stream = stream;
    }

    public void Deconstruct(out string path, out Stream stream)
    {
        path = this.Path;
        stream = this.Stream;
    }
}

public interface IFileSystem
{
    string Combine(params string[] paths);

    string GetDirectoryPath(string path);

    string GetFullPath(string path);

    bool IsPathRooted(string path);

    string ResolveRelativePath(string basePath, string path);

    Task<bool> IsFileExistsAsync(
        string path, CancellationToken ct);

    Task<string[]> GetFilesAsync(
        string basePath, string match, CancellationToken ct);

    Task<Stream> OpenAsync(
        string path, bool isSeekable, CancellationToken ct);

    Task<TemporaryFileDescriptor> CreateTemporaryAsync(
        CancellationToken ct);

    string GetFileName(string path);

    Task<bool> IsDirectoryExistsAsync(
        string path, CancellationToken ct);

    Task<string[]> GetDirectoryEntriesAsync(
        string path, CancellationToken ct);

    string GetRelativePath(string basePath, string path);

    string ToPosixPath(string path);
}
