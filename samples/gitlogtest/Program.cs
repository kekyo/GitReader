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

            var parentsTask = commit.GetParentCommitsAsync();

            var refs = string.Join(", ",
                commit.Tags.
                Select(t => $"tag: {t.Name}").
                Concat(commit.RemoteBranches.
                    Concat(commit.Branches).
                    Select(b => head?.Name == b.Name ? $"HEAD -> {b.Name}" : b.Name)).
                OrderBy(name => name, StringComparer.Ordinal).  // deterministic
                ToArray());

            if (first)
            {
                first = false;
            }
            else
            {
                Console.WriteLine();
            }

            if (refs.Length >= 1)
            {
                Console.WriteLine($"commit {commit.Hash} ({refs})");
            }
            else
            {
                Console.WriteLine($"commit {commit.Hash}");
            }

            var parents = await parentsTask;
            if (parents.Length >= 2)
            {
                var merge = string.Join(" ",
                    parents.
                    Select(p => p.Hash.ToString().Substring(0, 7)).
                    ToArray());

                Console.WriteLine($"Merge: {merge}");
            }

            Console.WriteLine($"Author: {commit.Author.ToGitAuthorString()}");
            Console.WriteLine($"Date:   {commit.Author.Date.ToGitDateString()}");

            Console.WriteLine();

            var lines = commit.Message.Split('\n');
            foreach (var line in
                lines.LastOrDefault()?.Length == 0 ?
                    lines.Take(lines.Length - 1) : lines)
            {
                Console.WriteLine("    " + line);
            }

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
