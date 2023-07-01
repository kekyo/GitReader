////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitReader.Collections;

[DebuggerDisplay("Count={Count}")]
public sealed class ReadOnlyArray<TValue> :
    IList<TValue>
#if !NET35 && !NET40
    , IReadOnlyList<TValue>
#endif
{
    internal readonly TValue[] parent;

    public ReadOnlyArray(TValue[] parent) =>
        this.parent = parent;

    public TValue this[int index] =>
        this.parent[index];

    public int Count =>
        this.parent.Length;

    internal bool Contains(TValue item) =>
        Array.IndexOf(this.parent, item) >= 0;

    bool ICollection<TValue>.Contains(TValue item) =>
        Array.IndexOf(this.parent, item) >= 0;

    internal int IndexOf(TValue item) =>
        Array.IndexOf(this.parent, item);

    int IList<TValue>.IndexOf(TValue item) =>
        this.IndexOf(item);

    public IEnumerator<TValue> GetEnumerator() =>
        ((IEnumerable<TValue>)this.parent).GetEnumerator();

    bool ICollection<TValue>.IsReadOnly =>
        true;

    TValue IList<TValue>.this[int index]
    {
        get => this.parent[index];
        set => throw new InvalidOperationException();
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        this.parent.GetEnumerator();

    void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex) =>
        ((ICollection<TValue>)this.parent).CopyTo(array, arrayIndex);

    internal TValue[] Clone()
    {
        var cloned = new TValue[this.parent.Length];
        Array.Copy(this.parent, cloned, this.parent.Length);
        return cloned;
    }

    public static implicit operator ReadOnlyArray<TValue>(TValue[] array) =>
        new(array);
    public static implicit operator ReadOnlyArray<TValue>(List<TValue> enumerable) =>
        new(enumerable.ToArray());

    ///////////////////////////////////////////////////////////////

    void IList<TValue>.Insert(int index, TValue item) =>
        throw new InvalidOperationException();

    void IList<TValue>.RemoveAt(int index) =>
        throw new InvalidOperationException();

    void ICollection<TValue>.Add(TValue item) =>
        throw new InvalidOperationException();

    void ICollection<TValue>.Clear() =>
        throw new InvalidOperationException();

    bool ICollection<TValue>.Remove(TValue item) =>
        throw new InvalidOperationException();
}
