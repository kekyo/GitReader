////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
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

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
    private static async Task CheckoutAsync(
        TextWriter tw,
        string repositoryPath, string commitId)
    {
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

        var commit = await repository.GetCommitAsync(commitId);
        var tree = await commit!.GetTreeRootAsync();

        Task ExtractTreeAsync(TreeEntry[] entries, string basePath) =>
            Task.WhenAll(
                entries.Select(async entry =>
                {
                    var path = Path.Combine(basePath, entry.Name);
                    switch (entry)
                    {
                        case TreeDirectoryEntry directory:
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            tw.WriteLine($"Created: {path}");
                            await ExtractTreeAsync(directory.Children, path);
                            break;
                        case TreeBlobEntry blob:
                            using (var stream = await blob.OpenBlobAsync())
                            {
                                using var fs = new FileStream(
                                    path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);
                                await stream.CopyToAsync(fs);
                                await fs.FlushAsync();
                            }
                            tw.WriteLine($"Extracted: {path}");
                            break;
                    }
                }));

        await ExtractTreeAsync(tree.Children, ".");
    }

    private static Task MainAsync(string[] args)
    {
        if (args.Length == 2)
        {
            return CheckoutAsync(Console.Out, args[0], args[1]);
        }

        Console.WriteLine("usage: gitcotest <path> <commit id>");
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
