////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace GitReader.Collections;

public static class DictionaryExtension
{
    public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary)
        where TKey : notnull =>
        new(dictionary);

    public static bool ContainsKey<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TKey key)
        where TKey : notnull =>
        dict.parent.ContainsKey(key);

    public static bool ContainsValue<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TValue value)
        where TKey : notnull =>
        dict.parent.ContainsValue(value);

    public static bool TryGetValue<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict,
        TKey key, out TValue value)
        where TKey : notnull =>
        dict.parent.TryGetValue(key, out value!);

    public static Dictionary<TKey, TValue> Clone<TKey, TValue>(
        this ReadOnlyDictionary<TKey, TValue> dict)
        where TKey : notnull =>
        new(dict.parent);
}
