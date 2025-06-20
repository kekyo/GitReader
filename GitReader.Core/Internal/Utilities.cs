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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

[DebuggerStepThrough]
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

[DebuggerStepThrough]
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

[DebuggerStepThrough]
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

[DebuggerStepThrough]
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

#if !DEBUG
[DebuggerStepThrough]
#endif
internal static class Utilities
{
    public static readonly bool IsWindows =
#if NETSTANDARD1_6
        !IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HOMEDRIVE"));
#else
        Environment.OSVersion.Platform.ToString().StartsWith("Win");
#endif

    public static readonly Encoding UTF8 = new UTF8Encoding(false, true);

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

    public static readonly TimeSpan Infinite =
#if NET35 || NET40
        new TimeSpan(0, 0, 0, 0, -1);
#else
        Timeout.InfiniteTimeSpan;
#endif

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
    private static class EnumHelper<TEnum>
        where TEnum : struct, Enum
    {
        private static readonly TypeCode typeCode;

        static EnumHelper()
        {
            var type = Enum.GetUnderlyingType(typeof(TEnum));
            if (type == typeof(int))
            {
                typeCode = TypeCode.Int32;
            }
            else if (type == typeof(ushort))
            {
                typeCode = TypeCode.UInt16;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static bool HasFlag(Enum enumValue, Enum flags) =>
            typeCode switch
            {
                TypeCode.Int32 =>
                    ((int)(object)(TEnum)enumValue & (int)(object)(TEnum)flags) == (int)(object)(TEnum)flags,
                TypeCode.UInt16 =>
                    ((ushort)(object)(TEnum)enumValue & (ushort)(object)(TEnum)flags) == (ushort)(object)(TEnum)flags,
                _ =>
                    throw new InvalidOperationException(),
            };
    }

    public static unsafe bool HasFlag<TEnum>(this TEnum enumValue, TEnum flags)
        where TEnum : struct, Enum =>
        EnumHelper<TEnum>.HasFlag(enumValue, flags);
#endif

    public static void MakeBigEndian(
        byte[] buffer, int index, int size)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(buffer, index, size);
        }
    }

    public static bool IsNullOrWhiteSpace(string? str) =>
#if NET35
        string.IsNullOrEmpty(str) || str!.All(char.IsWhiteSpace);
#else
        string.IsNullOrWhiteSpace(str);
#endif

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
    
    public static bool CollectionEqual<T>(this IEnumerable<T> a, IEnumerable<T> b) =>
        object.ReferenceEquals(a, b) ||
        a.SequenceEqual(b);

    [DebuggerStepThrough]
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

    [DebuggerStepThrough]
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

    [DebuggerStepThrough]
    public static IEnumerable<T> Traverse<T>(
        this T value,
        Func<T, T?> selector)
        where T : class
    {
        while (true)
        {
            yield return value;
            if (selector(value) is not { } selected)
            {
                break;
            }
            value = selected;
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

    public static string? Tokenize(string str, char[] separators, ref int index)
    {
        var start = index;

        while (true)
        {
            if (index >= str.Length)
            {
                return null;
            }

            var ch = str[index];
            if (Array.IndexOf(separators, ch) == -1)
            {
                break;
            }

            index++;
        }

        do
        {
            var ch = str[index];
            if (Array.IndexOf(separators, ch) >= 0)
            {
                break;
            }

            index++;
        }
        while (index < str.Length);

        return str.Substring(start, index - start);
    }

#if DEBUG
    [DebuggerStepThrough]
    public static async Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks)
    {
        // Sequential execution on `Debug` conditional.
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        return results.ToArray();
    }

    [DebuggerStepThrough]
    public static async Task<T[]> WhenAll<T>(params Task<T>[] tasks)
    {
        var results = new T[tasks.Length];
        for (var index = 0; index < tasks.Length; index++)
        {
            results[index] = await tasks[index];
        }
        return results;
    }

    [DebuggerStepThrough]
    public static async Task WhenAll(IEnumerable<Task> tasks)
    {
        // Sequential execution on `Debug` conditional.
        foreach (var task in tasks)
        {
            await task;
        }
    }

    [DebuggerStepThrough]
    public static async Task WhenAll(params Task[] tasks)
    {
        // Sequential execution on `Debug` conditional.
        foreach (var task in tasks)
        {
            await task;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    [DebuggerStepThrough]
    public static async ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks)
    {
        // Sequential execution on `Debug` conditional.
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        return results.ToArray();
    }

    [DebuggerStepThrough]
    public static async ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks)
    {
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        return results.ToArray();
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    [DebuggerStepThrough]
    public static async ValueTask WhenAll(IEnumerable<ValueTask> tasks)
    {
        // Sequential execution on `Debug` conditional.
        foreach (var task in tasks)
        {
            await task;
        }
    }

    [DebuggerStepThrough]
    public static async ValueTask WhenAll(params ValueTask[] tasks)
    {
        foreach (var task in tasks)
        {
            await task;
        }
    }
#endif
#endif
#else
#if NET35 || NET40
    [DebuggerStepThrough]
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        TaskEx.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task<T[]> WhenAll<T>(params Task<T>[] tasks) =>
        TaskEx.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task WhenAll(IEnumerable<Task> tasks) =>
        TaskEx.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task WhenAll(params Task[] tasks) =>
        TaskEx.WhenAll(tasks);
#else
    [DebuggerStepThrough]
    public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
        Task.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task<T[]> WhenAll<T>(params Task<T>[] tasks) =>
        Task.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task WhenAll(IEnumerable<Task> tasks) =>
        Task.WhenAll(tasks);

    [DebuggerStepThrough]
    public static Task WhenAll(params Task[] tasks) =>
        Task.WhenAll(tasks);

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    [DebuggerStepThrough]
    public static ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks) =>
        WhenAll(
            // Implicit starting ValueTask'ed state machines just now.
            tasks.ToArray()
        );

    [DebuggerStepThrough]
    public static async ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks)
    {
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task);
        }
        return results.ToArray();
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    [DebuggerStepThrough]
    public static ValueTask WhenAll(IEnumerable<ValueTask> tasks) =>
        WhenAll(
            // Implicit starting ValueTask'ed state machines just now.
            tasks.ToArray()
        );

    [DebuggerStepThrough]
    public static async ValueTask WhenAll(params ValueTask[] tasks)
    {
        foreach (var task in tasks)
        {
            await task;
        }
    }
#endif
#endif
#endif
#endif

    [DebuggerStepThrough]
    public static async Task<PairResult<T0, T1>> Join<T0, T1>(
        Task<T0> task0, Task<T1> task1) =>
        new(await task0, await task1);

    [DebuggerStepThrough]
    public static async Task<PairResult<T0, T1, T2>> Join<T0, T1, T2>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2) =>
        new(await task0, await task1, await task2);

    [DebuggerStepThrough]
    public static async Task<PairResult<T0, T1, T2, T3>> Join<T0, T1, T2, T3>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2, Task<T3> task3) =>
        new(await task0, await task1, await task2, await task3);

    [DebuggerStepThrough]
    public static async Task<PairResult<T0, T1, T2, T3, T4>> Join<T0, T1, T2, T3, T4>(
        Task<T0> task0, Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4) =>
        new(await task0, await task1, await task2, await task3, await task4);

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    [DebuggerStepThrough]
    public static async ValueTask<PairResult<T0, T1>> Join<T0, T1>(
        ValueTask<T0> task0, ValueTask<T1> task1) =>
        new(await task0, await task1);

    [DebuggerStepThrough]
    public static async ValueTask<PairResult<T0, T1, T2>> Join<T0, T1, T2>(
        ValueTask<T0> task0, ValueTask<T1> task1, ValueTask<T2> task2) =>
        new(await task0, await task1, await task2);

    [DebuggerStepThrough]
    public static async ValueTask<PairResult<T0, T1, T2, T3>> Join<T0, T1, T2, T3>(
        ValueTask<T0> task0, ValueTask<T1> task1, ValueTask<T2> task2, ValueTask<T3> task3) =>
        new(await task0, await task1, await task2, await task3);

    [DebuggerStepThrough]
    public static async ValueTask<PairResult<T0, T1, T2, T3, T4>> Join<T0, T1, T2, T3, T4>(
        ValueTask<T0> task0, ValueTask<T1> task1, ValueTask<T2> task2, ValueTask<T3> task3, ValueTask<T4> task4) =>
        new(await task0, await task1, await task2, await task3, await task4);
#endif

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
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<T> WaitAsync<T>(
        this Task<T> task, CancellationToken ct)
#else
    public static async Task<T> WaitAsync<T>(
        this Task<T> task, CancellationToken ct)
#endif
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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask WaitAsync(
        this Task task, CancellationToken ct)
#else
    public static async Task WaitAsync(
        this Task task, CancellationToken ct)
#endif
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

#if NET35
    public static Task CopyToAsync(
        this Stream from, Stream to, int bufferSize, BufferPool pool, CancellationToken ct) =>
        Task.Factory.StartNew(() =>
        {
            using var buffer = pool.Take(bufferSize);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var read = from.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                ct.ThrowIfCancellationRequested();

                to.Write(buffer, 0, read);
            }
        }, ct);
#else
    public static Task CopyToAsync(
        this Stream from, Stream to, int bufferSize, BufferPool pool, CancellationToken ct) =>
        from.CopyToAsync(to, bufferSize, ct);
#endif

    public static int GetProcessId() =>
#if NET5_0_OR_GREATER
        Environment.ProcessId;
#else
        Process.GetCurrentProcess().Id;
#endif

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

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    public static async ValueTask<Hash> CalculateGitBlobHashAsync(
        Stream stream, long size, BufferPool bufferPool, CancellationToken ct)
#else
    public static async Task<Hash> CalculateGitBlobHashAsync(
        Stream stream, long size, BufferPool bufferPool, CancellationToken ct)
#endif
    {
        // Git calculates hash as: "blob <size>\0<content>"
        var header = UTF8.GetBytes($"blob {size}\0");

#if NETFRAMEWORK
        using var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
        using var combinedStream = new MemoryStream();
        combinedStream.Write(header, 0, header.Length);
        await CopyToAsync(stream, combinedStream, 65536, bufferPool, ct);
        var hash = sha1.ComputeHash(combinedStream.ToArray());
        return new Hash(hash);
#else
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        
        // For frameworks that don't support TransformBlock, use a memory stream approach
        using var combinedStream = new MemoryStream();
        combinedStream.Write(header, 0, header.Length);
        await CopyToAsync(stream, combinedStream, 65536, bufferPool, ct);
        var hash = sha1.ComputeHash(combinedStream.ToArray());
        return new Hash(hash);
#endif
    }
}
