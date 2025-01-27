﻿////////////////////////////////////////////////////////////////////////////
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

[DebuggerDisplay("Count={Count}")]
public sealed class ReadOnlyDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>
#if !NET35 && !NET40
    , IReadOnlyDictionary<TKey, TValue>
#endif
    where TKey : notnull
{
    internal readonly Dictionary<TKey, TValue> parent;

    public ReadOnlyDictionary(Dictionary<TKey, TValue> parent) =>
        this.parent = parent;

    public TValue this[TKey key] =>
        this.parent[key];

    public Dictionary<TKey, TValue>.KeyCollection Keys =>
        this.parent.Keys;

    public Dictionary<TKey, TValue>.ValueCollection Values =>
        this.parent.Values;

    public int Count =>
        this.parent.Count;

    internal bool ContainsKey(TKey key) =>
        this.parent.ContainsKey(key);

    internal bool ContainsValue(TValue value) =>
        this.parent.ContainsValue(value);

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
