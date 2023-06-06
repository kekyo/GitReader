////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using GitReader;
using GitReader.Primitive;
using GitReader.Structures;

[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net70)]
[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net60)]
[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net48)]
public class GitRepo
{
    private string gitPath = string.Empty;
     
    [GlobalSetup]
    public void GlobalSetup()
    {
        gitPath = Environment.GetEnvironmentVariable("GIT_PATH") ?? throw new InvalidOperationException("Please provide the path to a .git repository in command line");
    }
    
    [Benchmark]
    public async Task GitReader()
    {
        using var repository = await Repository.Factory.OpenPrimitiveAsync(gitPath);
        var stashes = await repository.GetStashesAsync();
        var branches = await repository.GetBranchHeadReferencesAsync();
        var remoteBranches = await repository.GetRemoteBranchHeadReferencesAsync();
        var tags = await repository.GetTagReferencesAsync();
    }

    [Benchmark]
    public async Task GitReaderStructured()
    {
        using var repository = await Repository.Factory.OpenStructureAsync(gitPath);
        var stashes = repository.Stashes.ToArray();
        var branches = repository.Branches.Values.ToArray();
        var tags = repository.Tags.Values.ToArray();
        var currentBranch = repository.GetCurrentHead();
    }

    [Benchmark]
    public void LibGit2sharp()
    {
        var repository = new LibGit2Sharp.Repository(gitPath);
        var stashes = repository.Stashes.ToArray();
        var branches = repository.Branches.ToArray();
        var tags = repository.Tags.ToArray();
        var currentBranch = repository.Head;
        var currentTrackedBranch = repository.Head?.TrackedBranch;
        var currentTrackingDetails = repository.Head?.TrackedBranch?.TrackingDetails;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        if (args is [ var repoPath])
        {
            args = new[] { "--filter", "*GitRepo*", "--envVars", $"GIT_PATH:{repoPath}" };
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
        else
        {
            Console.WriteLine("Please provide the path to a .git repository in command line");
        }
    }
}