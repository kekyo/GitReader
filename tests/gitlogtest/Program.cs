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
using System.Linq;
using System.Threading.Tasks;
using GitReader.Structures;

namespace GitReader;

public static class Program
{
    private static async Task WriteLogAsync(string repositoryPath)
    {
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

        var head = repository.GetHead();
        var commits = new HashSet<Commit>();
        if (head?.Head is { } c)
        {
            commits.Add(c);
        }

        while (commits.Count >= 1)
        {
            var commit = commits.OrderBy(c => c.Committer.Date).Last();

            var parentsTask = commit.GetParentCommitsAsync();

            var refs = string.Join(", ",
                commit.Tags.
                Select(t => $"tag: {t.Name}").
                OrderBy(name => name).
                Concat(commit.RemoteBranches.
                    Concat(commit.Branches).
                    Select(b => head?.Name == b.Name ? $"HEAD -> {b.Name}" : b.Name).
                    OrderBy(name => name)).
                ToArray());

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

            Console.WriteLine();

            commits.Remove(commit);

            foreach (var parent in parents)
            {
                commits.Add(parent);
            }
        }
    }

    private static async Task FixLogAsync(string logPath)
    {
        while (true)
        {
            var line = await Console.In.ReadLineAsync();
            if (line == null)
            {
                break;
            }

            if (line.StartsWith("commit "))
            {
                var startIndex = line.IndexOf('(');
                if (startIndex >= 0)
                {

                }
            }
            else
            {
                await Console.Out.WriteLineAsync(line);
            }
        }
    }

    public static Task Main(string[] args)
    {
        if (args.Length == 2)
        {
            if (args[0] == "-f")
            {
                return FixLogAsync(args[1]);
            }
            else
            {
                return WriteLogAsync(args[1]);
            }
        }

        Console.WriteLine("usage: gitlogtest [-f|-w] <path>");
        return Task.CompletedTask;
    }
}
