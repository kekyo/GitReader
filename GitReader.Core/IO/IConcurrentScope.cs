////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.IO;

/// <summary>
/// Interface for concurrent scope that controls parallel task execution.
/// </summary>
public interface IConcurrentScope
{
    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed</returns>
    Task WhenAll(CancellationToken ct, IEnumerable<Task> tasks);
    
    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete, returning their results.
    /// </summary>
    /// <typeparam name="T">The type of result returned by each task</typeparam>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed, containing an array of their results</returns>
    Task<T[]> WhenAll<T>(CancellationToken ct, IEnumerable<Task<T>> tasks);
}

/// <summary>
/// Extension methods for IConcurrentScope interface.
/// </summary>
public static class ConcurrentScopeExtension
{
    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete.
    /// </summary>
    /// <param name="concurrentScope">The concurrent scope instance</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed</returns>
    public static Task WhenAll(
        this IConcurrentScope concurrentScope,
        IEnumerable<Task> tasks) =>
        concurrentScope.WhenAll(default, tasks);

    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete, returning their results.
    /// </summary>
    /// <typeparam name="T">The type of result returned by each task</typeparam>
    /// <param name="concurrentScope">The concurrent scope instance</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed, containing an array of their results</returns>
    public static Task<T[]> WhenAll<T>(
        this IConcurrentScope concurrentScope,
        IEnumerable<Task<T>> tasks) =>
        concurrentScope.WhenAll(default, tasks);

    private static async Task<object?> RunInJoin<T>(Task<T> task) =>
        await task;

    public static async Task<PairResult<T0, T1>> Join<T0, T1>(
        this IConcurrentScope concurrentScope,
        Task<T0> task0, Task<T1> task1)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1)]);
        return new((T0)results[0]!, (T1)results[1]!);
    }

    public static async Task<PairResult<T0, T1, T2>> Join<T0, T1, T2>(
        this IConcurrentScope concurrentScope,
        Task<T0> task0, Task<T1> task1, Task<T2> task2)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1), RunInJoin(task2)]);
        return new((T0)results[0]!, (T1)results[1]!, (T2)results[2]!);
    }

    public static async Task<PairResult<T0, T1, T2, T3>> Join<T0, T1, T2, T3>(
        this IConcurrentScope concurrentScope,
        Task<T0> task0, Task<T1> task1, Task<T2> task2, Task<T3> task3)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1), RunInJoin(task2), RunInJoin(task3)]);
        return new((T0)results[0]!, (T1)results[1]!, (T2)results[2]!, (T3)results[3]!);
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private static async Task<object?> RunInJoin<T>(ValueTask<T> task) =>
        await task;

    internal static async Task<PairResult<T0, T1>> Join<T0, T1>(
        this IConcurrentScope concurrentScope,
        ValueTask<T0> task0, ValueTask<T1> task1)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1)]);
        return new((T0)results[0]!, (T1)results[1]!);
    }

    internal static async Task<PairResult<T0, T1, T2>> Join<T0, T1, T2>(
        this IConcurrentScope concurrentScope,
        ValueTask<T0> task0, ValueTask<T1> task1, ValueTask<T2> task2)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1), RunInJoin(task2)]);
        return new((T0)results[0]!, (T1)results[1]!, (T2)results[2]!);
    }

    internal static async Task<PairResult<T0, T1, T2, T3>> Join<T0, T1, T2, T3>(
        this IConcurrentScope concurrentScope,
        ValueTask<T0> task0, ValueTask<T1> task1, ValueTask<T2> task2, ValueTask<T3> task3)
    {
        var results = await concurrentScope.WhenAll([RunInJoin(task0), RunInJoin(task1), RunInJoin(task2), RunInJoin(task3)]);
        return new((T0)results[0]!, (T1)results[1]!, (T2)results[2]!, (T3)results[3]!);
    }
#endif
}

/// <summary>
/// Concurrent scope for loosely limiting the number of concurrent tasks.
/// </summary>
public sealed class LooseConcurrentScope : IConcurrentScope
{
    /// <summary>
    /// Tracks the number of available execution seats for concurrent tasks.
    /// </summary>
    private int availableExecutionSeat;
    
    private int floorExecutionSeat;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="targetConcurrentCount">Target concurrent count</param>
    public LooseConcurrentScope(int targetConcurrentCount)
    {
        this.availableExecutionSeat = targetConcurrentCount;
        this.floorExecutionSeat = targetConcurrentCount;
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int AvailableExecutionSeat =>
        this.availableExecutionSeat;
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int FloorExecutionSeat =>
        this.floorExecutionSeat;

    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete.
    /// This implementation differs from Task.WhenAll() as it does not collect exceptions.
    /// </summary>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled</exception>
    public async Task WhenAll(CancellationToken ct, IEnumerable<Task> tasks)
    {
        // Since no collection is done for any exceptions at all,
        // the behavior is strictly different from Task.WhenAll().

        // Collection of tasks that are ready to be executed in the current batch
        var candidateTasks = new List<Task>();
        var enumerator = tasks.GetEnumerator();
        try
        {
            // Main loop: continue until all tasks are processed
            while (true)
            {
                // Inner loop: collect tasks up to the available execution seat limit
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // Try to get the next task from the enumerable
                    var isTaskAvailable = enumerator.MoveNext();
                    if (!isTaskAvailable)
                    {
                        // No more tasks.
                        break;
                    }

                    // This task is candidate.
                    // NOTE: The meaning of “Loose” is to always execute at least
                    // one of the candidate task (even if the execution sheet does not exist).
                    // Failure to do so will result in a deadlock on multiple calls of the same logical call context
                    // (this usually occurs on a re-entrant call).
                    // A more strict implementation would be to detect re-entry in the logical call context
                    // and execute only in that case.
                    candidateTasks.Add(enumerator.Current);

                    // Atomically decrement the available seat count
                    var availableExecutionSeat = Interlocked.Decrement(ref this.availableExecutionSeat);
                    this.floorExecutionSeat = Math.Min(availableExecutionSeat, this.floorExecutionSeat);
                    if (availableExecutionSeat < 0)
                    {
                        // No more available execution seats, aggregation is done.
                        break;
                    }
                }

                // Execute the collected candidate tasks based on their count
                // Candidate only one task.
                if (candidateTasks.Count == 1)
                {
                    try
                    {
                        // Execute one task directly.
                        await candidateTasks[0];
                    }
                    finally
                    {
                        // Returns an execution seat.
                        Interlocked.Increment(ref this.availableExecutionSeat);
                        candidateTasks.Clear();
                    }
                }
                // Candidate some tasks.
                else if (candidateTasks.Count >= 2)
                {
                    try
                    {
                        // Execute in parallel.
                        await Utilities.WhenAll(candidateTasks);
                    }
                    finally
                    {
                        // Returns execution seats.
                        Interlocked.Add(ref this.availableExecutionSeat, candidateTasks.Count);
                        candidateTasks.Clear();
                    }
                }
                else
                {
                    // No candidate tasks, we're done processing.
                    break;
                }
            }
        }
        finally
        {
            // Cleanup: ensure execution seats are returned even in case of exceptions
            if (candidateTasks.Count >= 1)
            {
                // Returns execution seats.
                Interlocked.Add(ref this.availableExecutionSeat, candidateTasks.Count);
            }

            enumerator.Dispose();
        }
    }

    /// <summary>
    /// Executes tasks with controlled concurrency and waits for all to complete, returning their results.
    /// This implementation differs from Task.WhenAll() as it does not collect exceptions.
    /// </summary>
    /// <typeparam name="T">The type of result returned by each task</typeparam>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <param name="tasks">Tasks to execute concurrently</param>
    /// <returns>A task that completes when all input tasks have completed, containing an array of their results</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled</exception>
    public async Task<T[]> WhenAll<T>(CancellationToken ct, IEnumerable<Task<T>> tasks)
    {
        // Since no collection is done for any exceptions at all,
        // the behavior is strictly different from Task.WhenAll().

        // Collection of tasks that are ready to be executed in the current batch
        var candidateTasks = new List<Task<T>>();
        var enumerator = tasks.GetEnumerator();
        try
        {
            // Accumulator for results from completed tasks
            var results = new List<T>();
            
            // Main loop: continue until all tasks are processed
            while (true)
            {
                // Inner loop: collect tasks up to the available execution seat limit
                while (true)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // Try to get the next task from the enumerable
                    var isTaskAvailable = enumerator.MoveNext();
                    if (!isTaskAvailable)
                    {
                        // No more tasks.
                        break;
                    }

                    // This task is candidate.
                    // NOTE: The meaning of “Loose” is to always execute at least
                    // one of the candidate task (even if the execution sheet does not exist).
                    // Failure to do so will result in a deadlock on multiple calls of the same logical call context
                    // (this usually occurs on a re-entrant call).
                    // A more strict implementation would be to detect re-entry in the logical call context
                    // and execute only in that case.
                    candidateTasks.Add(enumerator.Current);

                    // Atomically decrement the available seat count
                    var availableExecutionSeat = Interlocked.Decrement(ref this.availableExecutionSeat);
                    this.floorExecutionSeat = Math.Min(availableExecutionSeat, this.floorExecutionSeat);
                    if (availableExecutionSeat < 0)
                    {
                        // No more available execution seats, aggregation is done.
                        break;
                    }
                }

                // Execute the collected candidate tasks based on their count
                // Candidate only one task.
                if (candidateTasks.Count == 1)
                {
                    try
                    {
                        // Execute one task directly and collect its result.
                        results.Add(await candidateTasks[0]);
                    }
                    finally
                    {
                        // Returns an execution seat.
                        Interlocked.Increment(ref this.availableExecutionSeat);
                        candidateTasks.Clear();
                    }
                }
                // Candidate some tasks.
                else if (candidateTasks.Count >= 2)
                {
                    try
                    {
                        // Execute in parallel and collect all results.
                        results.AddRange(await Utilities.WhenAll(candidateTasks));
                    }
                    finally
                    {
                        // Returns execution seats.
                        Interlocked.Add(ref this.availableExecutionSeat, candidateTasks.Count);
                        candidateTasks.Clear();
                    }
                }
                else
                {
                    // No candidate tasks, we're done processing.
                    break;
                }
            }
            // Return all collected results as an array
            return results.ToArray();
        }
        finally
        {
            // Cleanup: ensure execution seats are returned even in case of exceptions
            if (candidateTasks.Count >= 1)
            {
                // Returns execution seats.
                Interlocked.Add(ref this.availableExecutionSeat, candidateTasks.Count);
            }

            enumerator.Dispose();
        }
    }
    
    /// <summary>
    /// Default concurrent instance.
    /// </summary>
    public static readonly LooseConcurrentScope Default =
        // Exactly, it should be a value that depends on the I/O parallel degree that the machine can accept,
        // but it is very difficult to determine it automatically.
        // Therefore, we use the number of processors x2 as a substitute.
        // * https://learn.microsoft.com/en-us/windows/win32/fileio/i-o-completion-ports
        // * Mark Russinovich, Windows Internals 2nd edition
        new LooseConcurrentScope(Environment.ProcessorCount * 2);
}
