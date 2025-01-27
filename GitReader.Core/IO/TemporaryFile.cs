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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;



#if !NETSTANDARD1_6
using System.Runtime.ConstrainedExecution;
#endif

namespace GitReader.IO;

internal sealed class TemporaryFile :
#if !NETSTANDARD1_6
    CriticalFinalizerObject,
#endif
    IDisposable
{
    private GCHandle pathHandle;
    private GCHandle streamHandle;

    private TemporaryFile(
        string path, Stream stream)
    {
        this.pathHandle = GCHandle.Alloc(path, GCHandleType.Normal);
        this.streamHandle = GCHandle.Alloc(stream, GCHandleType.Normal);

        Debug.WriteLine($"GitReader: TemporaryFile: Created: {path}");
    }

    ~TemporaryFile() =>
        Dispose();

    public void Dispose()
    {
        if (this.streamHandle.IsAllocated &&
            this.streamHandle.Target is Stream stream)
        {
            this.streamHandle.Free();
            stream.Dispose();
        }

        if (this.pathHandle.IsAllocated &&
            this.pathHandle.Target is string path)
        {
            this.pathHandle.Free();
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
            Debug.WriteLine($"GitReader: TemporaryFile: Deleted: {path}");
        }

        GC.SuppressFinalize(this);
    }

    public Stream Stream =>
        (Stream)this.streamHandle.Target!;

    public string Path =>
        (string)this.pathHandle.Target!;

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<TemporaryFile> CreateFileAsync(
        IFileSystem fileSystem, CancellationToken ct)
#else
    public static async Task<TemporaryFile> CreateFileAsync(
        IFileSystem fileSystem, CancellationToken ct)
#endif
    {
        var (path, stream) = await fileSystem.CreateTemporaryAsync(ct);
        return new TemporaryFile(path, stream);
    }
}
