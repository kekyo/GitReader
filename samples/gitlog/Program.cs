////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

// This sample code mimics the standard log output of `git log --all`.
// Not exactly the same:
// * Basically, it is in descending date order, but the sorting algorithm is not the same as in `git log`,
//   so the order of the log output may be slightly different.
// * The format is slightly different. For hash values in merge commits, all digits are output.
// * The trim of the trailing newline in the message part is not exactly the same.

using GitReader.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;

namespace GitReader;

// Compares the `Commit` class. There are two functions:
// * Checks if instances match, using only SHA1 hash; used in HashSet.
// * Allows sorting by Committer date in descending order; used in SortedDictionary.
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

// Keep `Commit` sorted by date in descending order.
// The reason this class is needed is that there can be multiple commits with the exact same date.
// SortedDictionary sorts the `Commit` in descending order by date,
// so that the most recent commit group can be retrieved.
// Since there can be multiple commits, only the first one in particular can be referenced as a candidate.
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

    // One of the first commits.
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
    // Remove blank lines at the end of the message, handle indentation and tabs, to output.
    private static void WriteMessage(
        TextWriter tw,
        string message)
    {
        var lines = message.Split('\n');
        var blankLines = 0;

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                blankLines++;
                continue;
            }

            while (blankLines >= 1)
            {
                tw.WriteLine("    ");
                blankLines--;
            }

            // Expand tabs (ts = 8)
            var sb = new StringBuilder();
            for (var index = 0; index < line.Length; index++)
            {
                if (line[index] == '\t')
                {
                    var remains = 8 - index % 8;
                    while (remains > 0)
                    {
                        sb.Append(' ');
                        remains--;
                    }
                }
                else
                {
                    sb.Append(line[index]);
                }
            }

            tw.WriteLine("    " + sb);
        }
    }

    // Outputs the specified `Commit` object in a format similar to the `git log` command.
    // Returns the parent commit group referenced internally.
    private static async Task<Commit[]> WriteLogAsync(
        TextWriter tw,
        Commit commit,
        Branch? head,
        CancellationToken ct)
    {
        // It takes a small amount of time to retrieve the parent commit group.
        // It's a HACK, not a good way. But it improves processing efficiency
        // by multiply executing Task instances by awaiting them later.
        // If you divert this code, be aware of the exception that is raised until await.
        // If you leave parentsTask alone, the result of asynchronous processing will be left behind.
        var parentsTask = commit.GetParentCommitsAsync(ct);

        // Enumerate the branches and tags associated with this commit.
        var refs = string.Join(", ",
            commit.Tags.
            Select(t => $"tag: {t.Name}").
            Concat(commit.Branches.
                // If a HEAD commit references a particular branch, this is where it is determined.
                Select(b => head?.Name == b.Name ? $"HEAD -> {b.Name}" : b.Name)).
            OrderBy(name => name, StringComparer.Ordinal).  // deterministic
            ToArray());

        if (refs.Length >= 1)
        {
            tw.WriteLine($"commit {commit.Hash} ({refs})");
        }
        else
        {
            tw.WriteLine($"commit {commit.Hash}");
        }

        // (Catch the results for asynchronous operation)
        var parents = await parentsTask;
        if (parents.Length >= 2)
        {
            var merge = string.Join(" ",
                parents.
                Select(p => p.Hash.ToString()).
                ToArray());

            tw.WriteLine($"Merge: {merge}");
        }

        // If you want to match the standard format used in Git, you can use these methods.
        tw.WriteLine($"Author: {commit.Author.ToGitAuthorString()}");
        tw.WriteLine($"Date:   {commit.Author.Date.ToGitDateString()}");

        tw.WriteLine();

        // If we are using the high-level interface,
        // the commit message is automatically split into `Subject` and `Body`.
        // Message is split into its respective properties when the message is split by an empty line.
        // If you really want to get the original message string,
        // use `GetMessage()` or use the primitive interface.
        WriteMessage(tw, commit.Subject);
        WriteMessage(tw, commit.Body);

        tw.WriteLine();

        return parents;
    }

    // Dump the entire contents of the specified local repository in `git log` format.
    private static async Task WriteLogAsync(
        TextWriter tw,
        string repositoryPath)
    {
        // Open the repository. This sample code uses a high-level interface.
        using var repository = await Repository.Factory.OpenStructureAsync(repositoryPath);

        // HashSet to check if the commit has already been output.
        // Initially, insert HEAD commit for all local branches and remote branches.
        var headCommits = await Task.WhenAll(
            repository.Branches.Values.
            Select(branch => branch.GetHeadCommitAsync()));

        var hashedCommits = new HashSet<Commit>(
            headCommits, CommitComparer.Instance);

        // SortedCommitMap to extract the next commit to be output.
        // Insert all of the above commit groups as the initial state.
        var sortedCommits = new SortedCommitMap(
            hashedCommits);

        // Insert all tag commits in hashedCommits and sortedCommits.
        // Although not common, Git tags can also be applied to trees and file objects.
        // Therefore, here we only extract tags applied to commits.
        foreach (var tag in repository.Tags.Values)
        {
            // Only commit tag.
            if (tag.Type == ObjectTypes.Commit)
            {
                var commit = await tag.GetCommitAsync();

                // Ignore commits that already exist.
                if (hashedCommits.Add(commit))
                {
                    sortedCommits.Add(commit);
                }
            }
        }

        // Also add a HEAD commit for the repository if exists.
        if (repository.Head is { } head)
        {
            var headCommit = await head.GetHeadCommitAsync();

            if (hashedCommits.Add(headCommit))
            {
                sortedCommits.Add(headCommit);
            }
        }

        // Core part of the output, continuing until all commits from sortedCommits are exhausted.
        while (sortedCommits.Contains)
        {
            // Get the commit that should be output next.
            var commit = sortedCommits.Front!;

            // Execute the output of this commit to obtain the parent commit group of this commit.
            var parents = await WriteLogAsync(tw, commit, repository.Head, default);

            // Remove this commit from sortedCommits because it is complete.
            sortedCommits.RemoveFront();

            // Add the resulting parent commits to hashedCommits and sortedCommits
            // in preparation for the next commit to be output.
            foreach (var parent in parents)
            {
                if (hashedCommits.Add(parent))
                {
                    sortedCommits.Add(parent);
                }
            }
        }
    }

    public static Task Main(string[] args)
    {
        if (args.Length == 1)
        {
            return WriteLogAsync(Console.Out, args[0]);
        }

        Console.WriteLine("usage: gitlog <path>");
        return Task.CompletedTask;
    }
}
