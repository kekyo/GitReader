////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace GitReader.Collections;

/// <summary>
/// Provides extension methods for dictionary and ReadOnlyDictionary operations.
/// </summary>
public static class DictionaryExtension
{
    /// <summary>
    /// Converts a Dictionary to a ReadOnlyDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to convert.</param>
    /// <returns>A ReadOnlyDictionary wrapping the original dictionary.</returns>
    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary)
        where TKey : notnull =>
        new(dictionary);

    /// <summary>
    /// Determines whether the ReadOnlyDictionary contains the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dict">The ReadOnlyDictionary to search.</param>
    /// <param name="key">The key to locate.</param>
    /// <returns>true if the dictionary contains the key; otherwise, false.</returns>
    public static bool ContainsKey<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TKey key)
        where TKey : notnull =>
        dict.parent.ContainsKey(key);

    /// <summary>
    /// Determines whether the ReadOnlyDictionary contains the specified value.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dict">The ReadOnlyDictionary to search.</param>
    /// <param name="value">The value to locate.</param>
    /// <returns>true if the dictionary contains the value; otherwise, false.</returns>
    public static bool ContainsValue<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TValue value)
        where TKey : notnull =>
        dict.parent.ContainsValue(value);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dict">The ReadOnlyDictionary to search.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public static bool TryGetValue<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TKey key, out TValue value)
        where TKey : notnull =>
        dict.parent.TryGetValue(key, out value!);

    /// <summary>
    /// Creates a shallow copy of the ReadOnlyDictionary as a regular Dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dict">The ReadOnlyDictionary to clone.</param>
    /// <returns>A new Dictionary containing copies of all key-value pairs.</returns>
    public static Dictionary<TKey, TValue> Clone<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict)
        where TKey : notnull =>
        new(dict.parent);
}
