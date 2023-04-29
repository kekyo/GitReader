////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Structures;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
    private static async Task FixLogAsync(string logPath)
    {
        using var fs = File.Open(logPath, FileMode.Open);
        var tr = new StreamReader(fs);

        Task? writeTask = null;
        while (true)
        {
            var line = await tr.ReadLineAsync();
            if (line == null)
            {
                break;
            }

            if (line.StartsWith("commit "))
            {
                var startIndex = line.IndexOf('(');
                if (startIndex >= 0)
                {
                    var endIndex = line.IndexOf(')', startIndex + 1);
                    if (endIndex > startIndex)
                    {
                        var before = line.Substring(0, startIndex - 1);
                        var names = line.Substring(startIndex + 1, endIndex - startIndex - 1).
                            Split(',').
                            Select(name => name.Trim()).
                            OrderBy(name => name, StringComparer.Ordinal).  // deterministic
                            ToArray();

                        writeTask = Console.Out.WriteLineAsync(
                            $"{before} ({string.Join(", ", names)})");
                        continue;
                    }
                }
            }

            writeTask = Console.Out.WriteLineAsync(line);
        }

        if (writeTask != null)
        {
            await writeTask;
        }

        await Console.Out.FlushAsync();
    }

    private static async Task WriteLogAsync(string repositoryPath)
    {
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

        var commits = new HashSet<Commit>(
            repository.RemoteBranches.Values.Select(branch => branch.Head));

        foreach (var tag in repository.Tags.Values.
            Select(tag => tag is CommitTag ct ? ct.Commit : null).
            Where(tag => tag != null))
        {
            commits.Add(tag!);
        }

        var head = repository.GetHead();
        if (head?.Head is { } c)
        {
            commits.Add(c);
        }

        var first = true;
        while (commits.Count >= 1)
        {
            var commit = commits.OrderBy(c => c.Committer.Date).Last();

            if (first)
            {
                first = false;
            }
            else
            {
                Console.Out.WriteLine();
            }

            var parents = await commit.PrettyPrintAsync(
                Console.Out, head, default);

            commits.Remove(commit);

            foreach (var parent in parents)
            {
                commits.Add(parent);
            }
        }
    }

    private static Task MainAsync(string[] args)
    {
        if (args.Length == 2)
        {
            if (args[0] == "--fix")
            {
                return FixLogAsync(args[1]);
            }
            else if (args[0] == "--log")
            {
                return WriteLogAsync(args[1]);
            }
        }

        Console.WriteLine("usage: gitlogtest [--fix|--log] <path>");
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
