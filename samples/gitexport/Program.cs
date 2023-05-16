////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

// This sample is similar to the `git checkout-index -a -f` command,
// which outputs a set of files for a given commit ID.

using GitReader.Structures;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace GitReader;

public static class Program
{
    [DebuggerStepThrough]
    public static async ValueTask WhenAll(IEnumerable<ValueTask> tasks)
    {
        foreach (var task in tasks.ToArray())
        {
            await task;
        }
    }

    private static async Task ExportAsync(
        TextWriter tw,
        string repositoryPath, string commitId, string toPath)
    {
        if (Directory.Exists(toPath))
        {
            Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);
        }

        var sw = Stopwatch.StartNew();

        using var repository =
            await Repository.Factory.OpenStructureAsync(repositoryPath);

        var opened = sw.Elapsed;
        tw.WriteLine($"Opened: {opened}");

        var commit = await repository.GetCommitAsync(commitId);

        var gotCommit = sw.Elapsed;
        tw.WriteLine($"Got commit: {gotCommit - opened}");

        var tree = await commit!.GetTreeRootAsync();

        var gotTree = sw.Elapsed;
        tw.WriteLine($"Got tree: {gotTree - gotCommit}");

        var directories = 0;
        var files = 0;

        static async ValueTask ExtractBlobAsync(
            TreeBlobEntry blob, string path)
        {
            var openBlobAsync = blob.OpenBlobAsync();

            // I don't know why basePath doesn't exist in randomly case.
            // The directory should always have been created by TreeDirectoryEntry before coming here.
            // There may be a potential problem in WSL environment...
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

            var stream = await openBlobAsync;

            using var fs = new FileStream(
                path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 65536, true);
            await stream.CopyToAsync(fs);
            await fs.FlushAsync();
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
                    default:
                        return default;
                }
            }));

        await ExtractTreeAsync(tree.Children, toPath);

        var extracted = sw.Elapsed;
        var e = extracted - gotTree;
        tw.WriteLine($"Extracted: {e}");

        tw.WriteLine();

        var dr = (double)directories / (directories + files);
        var d = TimeSpan.FromTicks((long)(e.Ticks * dr));
        tw.WriteLine($"Directories: {directories}, {d}");

        var fr = (double)files / (directories + files);
        var f = TimeSpan.FromTicks((long)(e.Ticks * fr));
        tw.WriteLine($"Files: {files}, {f}");
    }

    private static Task Main(string[] args)
    {
        if (args.Length == 3)
        {
            return ExportAsync(Console.Out, args[0], args[1], args[2]);
        }

        Console.WriteLine("usage: gitexport <repository path> <commit id> <to path>");
        return Task.CompletedTask;
    }
}
