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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class Utilities
{
    // Imported from corefx.
    private const long TicksPerMillisecond = 10000;
    private const long TicksPerSecond = TicksPerMillisecond * 1000;
    private const long TicksPerMinute = TicksPerSecond * 60;
    private const long TicksPerHour = TicksPerMinute * 60;
    private const long TicksPerDay = TicksPerHour * 24;
    private const int DaysPerYear = 365;
    private const int DaysPer4Years = DaysPerYear * 4 + 1;
    private const int DaysPer100Years = DaysPer4Years * 25 - 1;
    private const int DaysPer400Years = DaysPer100Years * 4 + 1;
    private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
    private const long UnixEpochTicks = DaysTo1970 * TicksPerDay;
    private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond;

    public static DateTimeOffset FromUnixTimeSeconds(long seconds, TimeSpan offset)
    {
        var ticks = seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;
        return new(new DateTime(ticks) + offset, offset);
    }

#if NET35 || NET40 || NET45
    public static long ToUnixTimeSeconds(this DateTimeOffset dateTime) =>
        dateTime.UtcDateTime.Ticks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
#endif

    public static DateTimeOffset TruncateMilliseconds(DateTimeOffset date) =>
        new(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Offset);

#if NET35
    public static bool TryParse<TEnum>(string str, bool ignoreCase, out TEnum value)
        where TEnum : struct, Enum
    {
        try
        {
            value = (TEnum)Enum.Parse(typeof(TEnum), str, ignoreCase);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
#else
    public static bool TryParse<TEnum>(string str, bool ignoreCase, out TEnum value)
        where TEnum : struct, Enum =>
        Enum.TryParse(str, ignoreCase, out value);
#endif

#if NET35
    public static IEnumerable<string> EnumerateFiles(string basePath, string match) =>
        Directory.GetFiles(basePath, match, SearchOption.AllDirectories);
#else
    public static IEnumerable<string> EnumerateFiles(string basePath, string match) =>
        Directory.EnumerateFiles(basePath, match, SearchOption.AllDirectories);
#endif

#if NET35
    public static string Combine(params string[] paths) =>
        paths.Aggregate(Path.Combine);
#else
    public static string Combine(params string[] paths) =>
        Path.Combine(paths);
#endif

    public static string GetDirectoryPath(string path) =>
        Path.GetDirectoryName(path) switch
        {
            // Not accurate in Windows, but a compromise...
            null => Path.DirectorySeparatorChar.ToString(),
            "" => string.Empty,
            var dp => dp,
        };

    public static void MakeBigEndian(
        byte[] buffer, int index, int size)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(buffer, index, size);
        }
    }

#if NET35 || NET40
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        TaskEx.WhenAll(tasks);
#else
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        Task.WhenAll(tasks);
#endif

    public readonly struct Pair<T0, T1>
    {
        public readonly T0 Item0;
        public readonly T1 Item1;

        public Pair(T0 item0, T1 item1)
        {
            this.Item0 = item0;
            this.Item1 = item1;
        }

        public void Deconstruct(out T0 item0, out T1 item1)
        {
            item0 = this.Item0;
            item1 = this.Item1;
        }
    }

    public static async Task<Pair<T0, T1>> WhenAll<T0, T1>(
        Task<T0> task0, Task<T1> task1) =>
        new(await task0, await task1);

#if NET35 || NET40
    public static Task<T> FromResult<T>(T result) =>
        TaskEx.FromResult(result);
#else
    public static Task<T> FromResult<T>(T result) =>
        Task.FromResult(result);
#endif

#if NET35 || NET40
    public static Task Delay(TimeSpan delay, CancellationToken ct) =>
        TaskEx.Delay(delay, ct);
#else
    public static Task Delay(TimeSpan delay, CancellationToken ct) =>
        Task.Delay(delay, ct);
#endif

#if !NET6_0_OR_GREATER
    public static async Task<T> WaitAsync<T>(
        this Task<T> task, CancellationToken ct)
    {
        if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
        {
            return task.GetAwaiter().GetResult();
        }

        var tcs = new TaskCompletionSource<T>();
        using var _ = ct.Register(() => tcs.TrySetCanceled());

        var __ = task.ContinueWith(t =>
        {
            if (!t.IsFaulted)
            {
                tcs.TrySetResult(t.Result);
            }
            else if (t.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetException(t.Exception!);
            }
        });

        return await tcs.Task;
    }

    public static async Task WaitAsync(
        this Task task, CancellationToken ct)
    {
        if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
        {
            // Consume continuation.
            task.GetAwaiter().GetResult();
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        using var _ = ct.Register(() => tcs.TrySetCanceled());

        var __ = task.ContinueWith(t =>
        {
            if (!t.IsFaulted)
            {
                tcs.TrySetResult(true);
            }
            else if (t.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetException(t.Exception!);
            }
        });

        await tcs.Task;
    }
#endif

#if NET35 || NET40
    public static Task<int> ReadAsync(
        this Stream stream,
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Factory.StartNew(() => stream.Read(buffer, offset, count));
    }
#endif

#if NET35 || NET40
    public static Task<string> ReadToEndAsync(
        this TextReader tr) =>
        Task.Factory.StartNew(tr.ReadToEnd);
#endif

#if NET35 || NET40
    public static Task<string?> ReadLineAsync(
        this TextReader tr) =>
        Task.Factory.StartNew(tr.ReadLine);
#endif

#if NET35
    public static Task WriteAsync(
        this TextWriter tw, string str) =>
        Task.Factory.StartNew(() => tw.Write(str));
#endif

#if NET35
    public static Task FlushAsync(
        this TextWriter tw) =>
        Task.Factory.StartNew(tw.Flush);
#endif

    public static int GetProcessId() =>
#if NET5_0_OR_GREATER
        Environment.ProcessId;
#else
        Process.GetCurrentProcess().Id;
#endif

    public static async Task<Stream> CreateZLibStreamAsync(
        Stream parent, CancellationToken ct)
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse zlib stream. Step={step}");

        var buffer = new byte[2];
        var read = await parent.ReadAsync(buffer, 0, buffer.Length, ct);

        if (read < 2)
        {
            Throw(1);
        }

        if (buffer[0] != 0x78)
        {
            Throw(2);
        }

        switch (buffer[1])
        {
            case 0x01:
            case 0x5e:
            case 0x9c:
            case 0xda:
                break;
            default:
                Throw(3);
                break;
        }

        return new DeflateStream(parent, CompressionMode.Decompress, false);
    }
}
