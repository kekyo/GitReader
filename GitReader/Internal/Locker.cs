////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
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

#if !NETSTANDARD1_6
using System.Runtime.ConstrainedExecution;
#endif

namespace GitReader.Internal;

internal sealed class Locker :
#if !NETSTANDARD1_6
    CriticalFinalizerObject,
#endif
    IDisposable
{
    private readonly string lockPath;
    private Stream? lockStream;
    private GCHandle lockPathHandle;
    private GCHandle lockStreamHandle;

    private Locker(
        string lockPath, Stream lockStream)
    {
        this.lockPath = lockPath;
        this.lockStream = lockStream;
        this.lockPathHandle = GCHandle.Alloc(lockPath, GCHandleType.Normal);
        this.lockStreamHandle = GCHandle.Alloc(lockStream, GCHandleType.Normal);
    }

    ~Locker() =>
        this.Dispose();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this.lockStream, null) is { } lockStream)
        {
            lockStream.Dispose();
            try
            {
                File.Delete(this.lockPath);
            }
            catch
            {
            }

            this.lockPathHandle.Free();
            this.lockStreamHandle.Free();

            GC.SuppressFinalize(this);
        }
    }

    public static async Task<Locker> CreateAsync(
        string lockPath, CancellationToken ct, bool forceUnlock)
    {
        while (true)
        {
            try
            {
                if (!File.Exists(lockPath))
                {
                    var lockStream = new FileStream(
                        lockPath,
                        forceUnlock ? FileMode.Create : FileMode.CreateNew,
                        FileAccess.ReadWrite,
                        FileShare.None);

                    var tw = new StreamWriter(lockStream, Encoding.UTF8);
                    await tw.WriteAsync(Utilities.GetProcessId().ToString()).WaitAsync(ct);
                    await tw.FlushAsync().WaitAsync(ct);

                    return new(lockPath, lockStream);
                }
            }
            catch
            {
            }

            await Utilities.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
