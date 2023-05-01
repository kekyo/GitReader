////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
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

internal readonly struct PairResult<T0, T1>
{
    public readonly T0 Item0;
    public readonly T1 Item1;

    public PairResult(T0 item0, T1 item1)
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

internal readonly struct PairResult<T0, T1, T2>
{
    public readonly T0 Item0;
    public readonly T1 Item1;
    public readonly T2 Item2;

    public PairResult(T0 item0, T1 item1, T2 item2)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
    }

    public void Deconstruct(out T0 item0, out T1 item1, out T2 item2)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
    }
}

internal readonly struct PairResult<T0, T1, T2, T3>
{
    public readonly T0 Item0;
    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;

    public PairResult(
        T0 item0, T1 item1, T2 item2, T3 item3)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
    }

    public void Deconstruct(
        out T0 item0, out T1 item1, out T2 item2, out T3 item3)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
    }
}

internal readonly struct PairResult<T0, T1, T2, T3, T4>
{
    public readonly T0 Item0;
    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
    public readonly T4 Item4;

    public PairResult(
        T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
        this.Item4 = item4;
    }

    public void Deconstruct(
        out T0 item0, out T1 item1, out T2 item2, out T3 item3, out T4 item4)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
        item4 = this.Item4;
    }
}

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

#if NET35 || NET40 || NET45
    private static class ArrayEmpty<T>
    {
        public static readonly T[] Empty = new T[0];
    }

    public static T[] Empty<T>() =>
        ArrayEmpty<T>.Empty;
#else
    public static T[] Empty<T>() =>
        Array.Empty<T>();
#endif

    public static IEnumerable<U> Collect<T, U>(
        this IEnumerable<T> enumerable,
        Func<T, U?> selector)
    {
        foreach (var item in enumerable)
        {
            if (selector(item) is U selected)
            {
                yield return selected;
            }
        }
    }

    public static IEnumerable<U> CollectValue<T, U>(
        this IEnumerable<T> enumerable,
        Func<T, U?> selector)
        where U : struct
    {
        foreach (var item in enumerable)
        {
            if (selector(item) is U selected)
            {
                yield return selected;
            }
        }
    }

#if !NET6_0_OR_GREATER
    private sealed class DistinctKeyComparer<T, TKey> : IEqualityComparer<T>
    {
        private readonly Func<T, TKey> selector;

        public DistinctKeyComparer(Func<T, TKey> selector) =>
            this.selector = selector;

        public bool Equals(T? x, T? y) =>
            x is { } && y is { } &&
            this.selector(x)!.Equals(this.selector(y));

        public int GetHashCode(T? obj) =>
            obj is { } ?
                this.selector(obj)!.GetHashCode() : 0;
    }

    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> enumerable, Func<T, TKey> selector) =>
        enumerable.Distinct(new DistinctKeyComparer<T, TKey>(selector));
#endif

    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary)
        where TKey : notnull =>
        new(dictionary);

#if NET35 || NET40
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        TaskEx.WhenAll(tasks);
#else
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        Task.WhenAll(tasks);
#endif

    public static async Task<PairResult<T0, T1>> WhenAll<T0, T1>(
        Task<T0> task0, Task<T1> task1) =>
        new(await task0, await task1);

    public static async Task<PairResult<T0, T1, T2>> WhenAll<T0, T1, T2>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2) =>
        new(await task0, await task1, await task2);

    public static async Task<PairResult<T0, T1, T2, T3>> WhenAll<T0, T1, T2, T3>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2, Task<T3> task3) =>
        new(await task0, await task1, await task2, await task3);

    public static async Task<PairResult<T0, T1, T2, T3, T4>> WhenAll<T0, T1, T2, T3, T4>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4) =>
        new(await task0, await task1, await task2, await task3, await task4);

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
        byte[] buffer, int offset, int count,
        CancellationToken ct) =>
        Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, ct);
#endif

#if NET35 || NET40
    public static Task WriteAsync(
        this Stream stream,
        byte[] buffer, int offset, int count,
        CancellationToken ct) =>
        Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, ct);
#endif

#if NET35
    public static Task FlushAsync(
        this Stream stream, CancellationToken ct) =>
        Task.Factory.StartNew(stream.Flush, ct);
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

    public static string ToGitDateString(
        DateTimeOffset date)
    {
        var zone = date.Offset.TotalSeconds >= 0 ?
                string.Format(CultureInfo.InvariantCulture, "+{0:hhmm}", date.Offset) :
                string.Format(CultureInfo.InvariantCulture, "-{0:hhmm}", date.Offset);
        return string.Format(CultureInfo.InvariantCulture, "{0:ddd MMM d HH:mm:ss yyyy} {1}", date, zone);
    }

    public static string ToGitIsoDateString(
        DateTimeOffset date)
    {
        var zone = date.Offset.TotalSeconds >= 0 ?
                string.Format(CultureInfo.InvariantCulture, "+{0:hhmm}", date.Offset) :
                string.Format(CultureInfo.InvariantCulture, "-{0:hhmm}", date.Offset);
        return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss} {1}", date, zone);
    }

    public static string ToGitAuthorString(
        Signature signature) =>
        signature.MailAddress is { } mailAddress ?
            $"{signature.Name} <{mailAddress}>" : signature.Name;
}
