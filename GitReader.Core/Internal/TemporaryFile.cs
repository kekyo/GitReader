////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

#if !NETSTANDARD1_6
using System.Runtime.ConstrainedExecution;
#endif

namespace GitReader.Internal;

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
        this.Dispose();

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

    public static TemporaryFile CreateFile()
    {
        var path = Utilities.Combine(
            System.IO.Path.GetTempPath(),
            System.IO.Path.GetTempFileName());

        var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None);

        return new TemporaryFile(path, stream);
    }
}
