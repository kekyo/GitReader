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
using System.Linq;

namespace GitReader.Collections;

/// <summary>
/// Represents a read-only array that provides a view over an underlying array.
/// </summary>
/// <typeparam name="TValue">The type of elements in the array.</typeparam>
[DebuggerDisplay("Count={Count}")]
public sealed class ReadOnlyArray<TValue> :
    IList<TValue>
#if !NET35 && !NET40
    , IReadOnlyList<TValue>
#endif
{
    internal readonly TValue[] parent;

    /// <summary>
    /// Initializes a new instance of the ReadOnlyArray class that wraps the specified array.
    /// </summary>
    /// <param name="parent">The array to wrap.</param>
    public ReadOnlyArray(TValue[] parent) =>
        this.parent = parent;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public TValue this[int index] =>
        this.parent[index];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
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

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
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

    /// <summary>
    /// Implicitly converts an array to a ReadOnlyArray.
    /// </summary>
    /// <param name="array">The array to convert.</param>
    /// <returns>A ReadOnlyArray wrapping the specified array.</returns>
    public static implicit operator ReadOnlyArray<TValue>(TValue[] array) =>
        new(array);
    
    /// <summary>
    /// Implicitly converts a List to a ReadOnlyArray.
    /// </summary>
    /// <param name="enumerable">The List to convert.</param>
    /// <returns>A ReadOnlyArray containing the elements from the List.</returns>
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
