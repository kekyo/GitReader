////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
    private static async Task ExtractObjectAsync(
        TextWriter tw,
        string repositoryPath, string objectId, string toPath)
    {
        var basePath = Path.GetDirectoryName(toPath) switch
        {
            null => Path.DirectorySeparatorChar.ToString(),
            "" => ".",
            string path => path,
        };
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        using var repository =
            await Repository.Factory.OpenPrimitiveAsync(repositoryPath);

        using var result = await repository.OpenRawObjectStreamAsync(objectId);

        using var fs = new FileStream(
            toPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

        await result.Stream.CopyToAsync(fs);
        await fs.FlushAsync();

        tw.WriteLine($"Extracted an object: Id={objectId}, Type={result.Type}, Path={toPath}");
    }

    private static Task MainAsync(string[] args)
    {
        if (args.Length == 3)
        {
            return ExtractObjectAsync(Console.Out, args[0], args[1], args[2]);
        }

        Console.WriteLine("usage: gitextracttest <repository path> <object id> <to path>");
        return Task.CompletedTask;
    }

#if DEBUG
    public static void Main(string[] args)
    {
        // Single-threaded for easier debugging of asynchronous operations.
        var sc = new SingleThreadedSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(sc);

        sc.Run(MainAsync(args));
    }
#else
    public static Task Main(string[] args) =>
        MainAsync(args);
#endif
}
