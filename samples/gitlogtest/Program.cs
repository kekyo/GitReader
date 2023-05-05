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
using System.Text;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

internal sealed class CommitComparer :
    IComparer<Commit>,
    IEqualityComparer<Commit>
{
    public int Compare(Commit? x, Commit? y) =>
        y!.Committer.Date.CompareTo(x!.Committer.Date);    // (Descendants)


    public bool Equals(Commit? x, Commit? y) =>
        x!.Hash.Equals(y!.Hash);

    public int GetHashCode(Commit obj) =>
        obj.Hash.GetHashCode();

    public static readonly CommitComparer Instance = new CommitComparer();
}

internal sealed class SortedCommitMap
{
    private readonly SortedDictionary<Commit, LinkedList<Commit>> dict =
        new(CommitComparer.Instance);

    public SortedCommitMap(IEnumerable<Commit> commits)
    {
        foreach (var commit in commits)
        {
            this.Add(commit);
        }
    }

    public bool Contains =>
        this.dict.Count >= 1;

    public int Count =>
        this.dict.Values.Sum(commit => commit.Count);

    public Commit? Front =>
        this.dict.Values.FirstOrDefault()?.First!.Value;

    public bool Add(Commit commit)
    {
        if (!this.dict.TryGetValue(commit, out var commits))
        {
            commits = new();
            this.dict.Add(commit, commits);
            commits.AddLast(commit);
            return true;
        }

        foreach (var c in commits)
        {
            if (c.Hash.Equals(commit.Hash))
            {
                return false;
            }
        }

        commits.AddLast(commit);
        return true;
    }

    public bool RemoveFront()
    {
        var commit = this.Front!;
        if (this.dict.TryGetValue(commit, out var commits))
        {
            for (var index = 0; index < commits.Count; index++)
            {
                foreach (var c in commits)
                {
                    if (c.Hash.Equals(commit.Hash))
                    {
                        commits.Remove(c);
                        if (commits.Count == 0)
                        {
                            this.dict.Remove(commit);
                        }
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

public static class Program
{
    private static async Task WriteLogAsync(string repositoryPath)
    {
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

#if false
        var hashedCommits = new HashSet<Commit>(CommitComparer.Instance);

        var c0 = await repository.GetCommitAsync("937b71cc8b5b998963a7f9a33312ba3549d55510");
        hashedCommits.Add(c0!);

        var sortedCommits = new SortedCommitMap(
            hashedCommits);
#else
        var hashedCommits = new HashSet<Commit>(
            repository.RemoteBranches.Values.Select(branch => branch.Head),
            CommitComparer.Instance);
        var sortedCommits = new SortedCommitMap(
            hashedCommits);

        foreach (var commit in repository.Tags.Values.
            Select(tag => tag is CommitTag ct ? ct.Commit : null).
            Where(commit => commit != null))
        {
            if (hashedCommits.Add(commit!))
            {
                sortedCommits.Add(commit!);
            }
        }

        var head = repository.GetHead();
        if (head?.Head is { } c)
        {
            if (hashedCommits.Add(c))
            {
                sortedCommits.Add(c);
            }
        }
#endif

        while (sortedCommits.Contains)
        {
            var commit = sortedCommits.Front!;

            var parents = await commit.GetParentCommitsAsync();

            Console.WriteLine(
                $"# {commit.Hash} {string.Join(" ", parents.Select(p => p.Hash).ToArray())} [{commit.Author.Name} <{commit.Author.MailAddress}> {commit.Author.Date.ToGitIsoDateString()}] [{commit.Committer.Name} <{commit.Committer.MailAddress}> {commit.Committer.Date.ToGitIsoDateString()}] {commit.Subject} {commit.Body}");

            sortedCommits.RemoveFront();

            foreach (var parent in parents)
            {
                if (hashedCommits.Add(parent))
                {
                    sortedCommits.Add(parent);
                }
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
