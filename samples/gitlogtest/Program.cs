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

public static class Program
{
    private sealed class CommitComparer :
        IComparer<DateTimeOffset>,
        IEqualityComparer<Commit>
    {
        public int Compare(DateTimeOffset x, DateTimeOffset y) =>
            y.CompareTo(x);  // descendants

        public bool Equals(Commit? x, Commit? y) =>
            x!.Hash.Equals(y!.Hash);

        public int GetHashCode(Commit obj) =>
            obj.Hash.GetHashCode();

        public static readonly CommitComparer Instance = new CommitComparer();
    }

    private sealed class SortedCommitMap
    {
        private readonly SortedDictionary<DateTimeOffset, List<Commit>> dict =
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
            this.dict.Values.FirstOrDefault()?[0];

        public bool Add(Commit commit)
        {
            if (!this.dict.TryGetValue(commit.Committer.Date, out var commits))
            {
                commits = new();
                this.dict.Add(commit.Committer.Date, commits);
                commits.Add(commit);
                return true;
            }

            foreach (var c in commits)
            {
                if (c.Hash.Equals(commit.Hash))
                {
                    return false;
                }
            }

            commits.Add(commit);
            return true;
        }

        public bool Remove(Commit commit)
        {
            if (this.dict.TryGetValue(commit.Committer.Date, out var commits))
            {
                for (var index = 0; index < commits.Count; index++)
                {
                    var c = commits[index];
                    if (c.Hash.Equals(commit.Hash))
                    {
                        commits.RemoveAt(index);
                        if (commits.Count == 0)
                        {
                            this.dict.Remove(commit.Committer.Date);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }

    private static async Task WriteLogAsync(string repositoryPath)
    {
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

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

        while (sortedCommits.Contains)
        {
            var commit = sortedCommits.Front!;

            var parents = await commit.GetParentCommitsAsync();

            Console.WriteLine(
                $"# {commit.Hash} {string.Join(" ", parents.Select(p => p.Hash).ToArray())} [{commit.Author.Name} <{commit.Author.MailAddress}> {commit.Author.Date.ToGitIsoDateString()}] [{commit.Committer.Name} <{commit.Committer.MailAddress}> {commit.Committer.Date.ToGitIsoDateString()}] {commit.Subject} {commit.Body}");

            sortedCommits.Remove(commit);

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
