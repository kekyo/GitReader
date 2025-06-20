////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace GitReader.Collections;

/// <summary>
/// Represents a read-only dictionary that provides a view over an underlying dictionary.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
[DebuggerDisplay("Count={Count}")]
public sealed class ReadOnlyDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>
#if !NET35 && !NET40
    , IReadOnlyDictionary<TKey, TValue>
#endif
    where TKey : notnull
{
    internal readonly Dictionary<TKey, TValue> parent;

    /// <summary>
    /// Initializes a new instance of the ReadOnlyDictionary class that wraps the specified dictionary.
    /// </summary>
    /// <param name="parent">The dictionary to wrap.</param>
    public ReadOnlyDictionary(Dictionary<TKey, TValue> parent) =>
        this.parent = parent;

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key.</returns>
    public TValue this[TKey key] =>
        this.parent[key];

    /// <summary>
    /// Gets a collection containing the keys of the dictionary.
    /// </summary>
    public Dictionary<TKey, TValue>.KeyCollection Keys =>
        this.parent.Keys;

    /// <summary>
    /// Gets a collection containing the values of the dictionary.
    /// </summary>
    public Dictionary<TKey, TValue>.ValueCollection Values =>
        this.parent.Values;

    /// <summary>
    /// Gets the number of key/value pairs contained in the dictionary.
    /// </summary>
    public int Count =>
        this.parent.Count;

    internal bool ContainsKey(TKey key) =>
        this.parent.ContainsKey(key);

    internal bool ContainsValue(TValue value) =>
        this.parent.ContainsValue(value);

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        this.parent.GetEnumerator();

    internal bool TryGetValue(TKey key, out TValue value) =>
        this.parent.TryGetValue(key, out value!);

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => this.parent[key];
        set => throw new NotImplementedException();
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys =>
        this.parent.Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values =>
        this.parent.Values;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly =>
        true;

#if !NET35 && !NET40
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        this.parent.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        this.parent.Values;
#endif

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)this.parent).Contains(item);

    bool IDictionary<TKey, TValue>.ContainsKey(TKey key) =>
        this.parent.ContainsKey(key);

    bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) =>
        this.parent.TryGetValue(key, out value!);

#if !NET35 && !NET40
    bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) =>
        this.parent.ContainsKey(key);

    bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) =>
        this.parent.TryGetValue(key, out value!);
#endif

    IEnumerator IEnumerable.GetEnumerator() =>
        this.parent.GetEnumerator();

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>)this.parent).CopyTo(array, arrayIndex);

    internal Dictionary<TKey, TValue> Clone() =>
        new(this.parent);

    /// <summary>
    /// Implicitly converts a Dictionary to a ReadOnlyDictionary.
    /// </summary>
    /// <param name="dict">The dictionary to convert.</param>
    /// <returns>A ReadOnlyDictionary wrapping the specified dictionary.</returns>
    public static implicit operator ReadOnlyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict) =>
        new(dict);

    ///////////////////////////////////////////////////////////////

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) =>
        throw new InvalidOperationException();

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        throw new InvalidOperationException();

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() =>
        throw new InvalidOperationException();

    bool IDictionary<TKey, TValue>.Remove(TKey key) =>
        throw new InvalidOperationException();

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
        throw new InvalidOperationException();
}
