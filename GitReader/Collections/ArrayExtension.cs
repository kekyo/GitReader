////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections;

/// <summary>
/// Provides extension methods for array and ReadOnlyArray operations.
/// </summary>
public static class ArrayExtension
{
    /// <summary>
    /// Converts an array to a ReadOnlyArray.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="array">The array to convert.</param>
    /// <returns>A ReadOnlyArray wrapping the original array.</returns>
    public static ReadOnlyArray<TValue> AsReadOnly<TValue>(
        this TValue[] array) =>
        new(array);

    /// <summary>
    /// Determines whether the ReadOnlyArray contains a specific value.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="array">The ReadOnlyArray to search.</param>
    /// <param name="item">The value to locate.</param>
    /// <returns>true if the array contains the item; otherwise, false.</returns>
    public static bool Contains<TValue>(
        this ReadOnlyArray<TValue> array,
        TValue item) =>
        array.Contains(item);

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="array">The ReadOnlyArray to search.</param>
    /// <param name="item">The value to locate.</param>
    /// <returns>The zero-based index of the first occurrence of item, or -1 if not found.</returns>
    public static int IndexOf<TValue>(
        this ReadOnlyArray<TValue> array,
        TValue item) =>
        array.IndexOf(item);

    /// <summary>
    /// Creates a shallow copy of the ReadOnlyArray as a regular array.
    /// </summary>
    /// <typeparam name="TValue">The type of elements in the array.</typeparam>
    /// <param name="array">The ReadOnlyArray to clone.</param>
    /// <returns>A new array containing copies of all elements.</returns>
    public static TValue[] Clone<TValue>(
        this ReadOnlyArray<TValue> array) =>
        array.Clone();
}
