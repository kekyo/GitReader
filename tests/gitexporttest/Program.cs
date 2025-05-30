﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Structures;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
    [DebuggerStepThrough]
    public static async ValueTask WhenAll(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks
#if !DEBUG
            .ToArray()
#endif
            )
        {
            await task;
        }
    }

    private static async Task CheckoutAsync(
        TextWriter tw,
        string repositoryPath, string commitId, string toPath)
    {
        if (Directory.Exists(toPath))
        {
            Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);
        }

        var sw = new Stopwatch();

        sw.Start();
        using var repository =
            await Repository.Factory.OpenStructureAsync(repositoryPath);
        sw.Stop();

        tw.WriteLine($"Opened: {sw.Elapsed}");

        sw.Restart();
        var commit = await repository.GetCommitAsync(commitId);
        sw.Stop();

        tw.WriteLine($"Got commit: {sw.Elapsed}");

        sw.Restart();
        var tree = await commit!.GetTreeRootAsync();
        sw.Stop();

        tw.WriteLine($"Got tree: {sw.Elapsed}");

        var directories = 0;
        var files = 0;

        static async ValueTask ExtractBlobAsync(
            TreeBlobEntry blob, string path)
        {
            var openBlobAsync = blob.OpenBlobAsync();

            var basePath = Path.GetDirectoryName(path)!;
            while (!Directory.Exists(basePath))
            {
                try
                {
                    Directory.CreateDirectory(basePath);
                }
                catch
                {
                }
            }

            using var fs = new FileStream(
                path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);

            var stream = await openBlobAsync;

            await stream.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        async ValueTask ExtractSubModule(TreeSubModuleEntry subModule, string basePath)
        {
            using var subModuleRepository = await subModule.OpenSubModuleAsync();

            var subModuleCommit = await subModuleRepository.GetCommitAsync(subModule);
            var subModuleRootTree = await subModuleCommit!.GetTreeRootAsync();

            await ExtractTreeAsync(subModuleRootTree.Children, basePath);
        }

        ValueTask ExtractTreeAsync(TreeEntry[] entries, string basePath) =>
            WhenAll(entries.Select(entry =>
            {
                var path = Path.Combine(basePath, entry.Name);
                switch (entry)
                {
                    case TreeDirectoryEntry directory:
                        Interlocked.Increment(ref directories);
                        while (!Directory.Exists(path))
                        {
                            try
                            {
                                Directory.CreateDirectory(path);
                            }
                            catch
                            {
                            }
                        }
                        return ExtractTreeAsync(directory.Children, path);
                    case TreeBlobEntry blob:
                        Interlocked.Increment(ref files);
                        return ExtractBlobAsync(blob, path);
                    case TreeSubModuleEntry subModule:
                        Interlocked.Increment(ref directories);
                        while (!Directory.Exists(path))
                        {
                            try
                            {
                                Directory.CreateDirectory(path);
                            }
                            catch
                            {
                            }
                        }
                        return ExtractSubModule(subModule, path);
                    default:
                        return default;
                }
            }));

        sw.Restart();
        await ExtractTreeAsync(tree.Children, toPath);
        sw.Stop();

        tw.WriteLine($"Extracted: {sw.Elapsed}");

        tw.WriteLine();

        tw.WriteLine($"Directories: {directories}");
        tw.WriteLine($"Files: {files}");
    }

    private static Task MainAsync(string[] args)
    {
        if (args.Length == 3)
        {
            return CheckoutAsync(Console.Out, args[0], args[1], args[2]);
        }

        Console.WriteLine("usage: gitcotest <repository path> <commit id> <to path>");
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
