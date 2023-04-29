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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
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

        while (commits.Count >= 1)
        {
            var commit = commits.OrderBy(c => c.Committer.Date).Last();

            var parents = await commit.GetParentCommitsAsync();

            var body = (commit.Body.Length == 0 || commit.Body.EndsWith("\n")) ?
                commit.Body : (commit.Body + '\n');

            Console.WriteLine(
                $"# {commit.Hash} {string.Join(" ", parents.Select(p => p.Hash).ToArray())} [{commit.Author.Name} <{commit.Author.MailAddress}> {commit.Author.Date.ToGitIsoDateString()}] [{commit.Committer.Name} <{commit.Committer.MailAddress}> {commit.Committer.Date.ToGitIsoDateString()}] {commit.Subject.Replace('\n', ' ')} {body}");

            commits.Remove(commit);

            foreach (var parent in parents)
            {
                commits.Add(parent);
            }
        }
    }

    private static Task MainAsync(string[] args)
    {
        if (args.Length == 1)
        {
            return WriteLogAsync(args[0]);
        }

        Console.WriteLine("usage: gitlogtest <path>");
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
