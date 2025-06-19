////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Structures;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

#if DEBUG
using Lepracaun;
#endif

namespace GitReader;

public static class Program
{
    private static async Task MeasureWorkingDirectoryStatusAsync(
        TextWriter tw,
        string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            tw.WriteLine($"Error: Repository path '{repositoryPath}' does not exist.");
            return;
        }

        var sw = new Stopwatch();

        tw.WriteLine($"Opening repository: {repositoryPath}");
        sw.Start();
        using var repository =
            await Repository.Factory.OpenStructureAsync(repositoryPath);
        sw.Stop();

        tw.WriteLine($"Repository open time: {sw.Elapsed}");

        // Execute Working Directory Status retrieval multiple times to calculate average time
        var iterations = 5;

        // 1. Measurement without filter
        tw.WriteLine($"\n=== Measurement without filter ===");
        var totalTimeNoFilter = TimeSpan.Zero;
        WorkingDirectoryStatus? statusNoFilter = null;

        tw.WriteLine($"Executing GetWorkingDirectoryStatusAsync() (no filter) {iterations} times...");

        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var status = await repository.GetWorkingDirectoryStatusAsync();
            sw.Stop();

            tw.WriteLine($"Run {i + 1}: {sw.Elapsed}");
            totalTimeNoFilter += sw.Elapsed;
            statusNoFilter = status;
        }

        var averageTimeNoFilter = new TimeSpan(totalTimeNoFilter.Ticks / iterations);
        tw.WriteLine($"Average execution time: {averageTimeNoFilter}");
        tw.WriteLine($"Total execution time: {totalTimeNoFilter}");

        // 2. Measurement with CreateCommonIgnoreFilter() applied
        tw.WriteLine($"\n=== Measurement with CreateCommonIgnoreFilter() applied ===");
        var totalTimeWithFilter = TimeSpan.Zero;
        WorkingDirectoryStatus? statusWithFilter = null;
        var commonIgnoreFilter = Glob.GetCommonIgnoreFilter();

        tw.WriteLine($"Executing GetWorkingDirectoryStatusWithFilterAsync() (CreateCommonIgnoreFilter) {iterations} times...");

        for (int i = 0; i < iterations; i++)
        {
            sw.Restart();
            var status = await repository.GetWorkingDirectoryStatusAsync(commonIgnoreFilter);
            sw.Stop();

            tw.WriteLine($"Run {i + 1}: {sw.Elapsed}");
            totalTimeWithFilter += sw.Elapsed;
            statusWithFilter = status;
        }

        var averageTimeWithFilter = new TimeSpan(totalTimeWithFilter.Ticks / iterations);
        tw.WriteLine($"Average execution time: {averageTimeWithFilter}");
        tw.WriteLine($"Total execution time: {totalTimeWithFilter}");

        // 3. Result comparison
        tw.WriteLine($"\n=== Result comparison ===");
        if (statusNoFilter != null && statusWithFilter != null)
        {
            tw.WriteLine("Without filter:");
            tw.WriteLine($"  Staged files count: {statusNoFilter.StagedFiles.Count}");
            tw.WriteLine($"  Modified files count: {statusNoFilter.UnstagedFiles.Count}");
            tw.WriteLine($"  Untracked files count: {statusNoFilter.UntrackedFiles.Count}");
            tw.WriteLine($"  Total files count: {statusNoFilter.StagedFiles.Count + statusNoFilter.UnstagedFiles.Count + statusNoFilter.UntrackedFiles.Count}");

            tw.WriteLine("With CreateCommonIgnoreFilter() applied:");
            tw.WriteLine($"  Staged files count: {statusWithFilter.StagedFiles.Count}");
            tw.WriteLine($"  Modified files count: {statusWithFilter.UnstagedFiles.Count}");
            tw.WriteLine($"  Untracked files count: {statusWithFilter.UntrackedFiles.Count}");
            tw.WriteLine($"  Total files count: {statusWithFilter.StagedFiles.Count + statusWithFilter.UnstagedFiles.Count + statusWithFilter.UntrackedFiles.Count}");

            // Calculate effectiveness
            var totalFilesNoFilter = statusNoFilter.StagedFiles.Count + statusNoFilter.UnstagedFiles.Count + statusNoFilter.UntrackedFiles.Count;
            var totalFilesWithFilter = statusWithFilter.StagedFiles.Count + statusWithFilter.UnstagedFiles.Count + statusWithFilter.UntrackedFiles.Count;
            var filteredFileCount = totalFilesNoFilter - totalFilesWithFilter;
            var filteringPercentage = totalFilesNoFilter > 0 ? (filteredFileCount * 100.0 / totalFilesNoFilter) : 0;

            tw.WriteLine($"\nFiltering effectiveness:");
            tw.WriteLine($"  Excluded files count: {filteredFileCount}");
            tw.WriteLine($"  Exclusion rate: {filteringPercentage:F1}%");

            // Performance improvement
            var timeDifference = averageTimeNoFilter - averageTimeWithFilter;
            var performanceImprovement = averageTimeNoFilter.TotalMilliseconds > 0 ? 
                (timeDifference.TotalMilliseconds / averageTimeNoFilter.TotalMilliseconds) * 100 : 0;

            tw.WriteLine($"\nPerformance improvement:");
            tw.WriteLine($"  Time reduction: {timeDifference}");
            tw.WriteLine($"  Improvement rate: {performanceImprovement:F1}%");

            // Display detailed results
            DisplayDetailedResults(tw, "Without filter", statusNoFilter);
            DisplayDetailedResults(tw, "With CreateCommonIgnoreFilter() applied", statusWithFilter);
        }
    }

    private static void DisplayDetailedResults(TextWriter tw, string title, WorkingDirectoryStatus status)
    {
        tw.WriteLine($"\n=== {title} - Details ===");

        // Staged files details
        if (status.StagedFiles.Count > 0)
        {
            tw.WriteLine("Staged files:");
            foreach (var file in status.StagedFiles)
            {
                tw.WriteLine($"  {file.Status}: {file.Path}");
            }
        }

        // Modified files details
        if (status.UnstagedFiles.Count > 0)
        {
            tw.WriteLine("Modified files:");
            foreach (var file in status.UnstagedFiles)
            {
                tw.WriteLine($"  {file.Status}: {file.Path}");
            }
        }

        // Untracked files details (display first 10 files only)
        if (status.UntrackedFiles.Count > 0)
        {
            tw.WriteLine("Untracked files:");
            var displayCount = Math.Min(10, status.UntrackedFiles.Count);
            for (int i = 0; i < displayCount; i++)
            {
                var file = status.UntrackedFiles[i];
                tw.WriteLine($"  {file.Status}: {file.Path}");
            }
            if (status.UntrackedFiles.Count > 10)
            {
                tw.WriteLine($"  ... and {status.UntrackedFiles.Count - 10} more files");
            }
        }
    }

    private static Task MainAsync(string[] args)
    {
        string repositoryPath;
        
        if (args.Length == 1)
        {
            repositoryPath = args[0];
        }
        else
        {
            // Use default path specified by user
            repositoryPath = "/home/kouji/Projects/RJK.PolyFit";
            Console.WriteLine($"No arguments specified. Using default path: {repositoryPath}");
        }

        return MeasureWorkingDirectoryStatusAsync(Console.Out, repositoryPath);
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