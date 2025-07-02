////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;
using System.Diagnostics;

namespace GitReader.IO;

/// <summary>
/// Represents a pair of two items.
/// </summary>
/// <typeparam name="T0">The type of the first item.</typeparam>
/// <typeparam name="T1">The type of the second item.</typeparam>
[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1>
{
    /// <summary>
    /// The first item.
    /// </summary>
    public readonly T0 Item0;

    /// <summary>
    /// The second item.
    /// </summary>
    public readonly T1 Item1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairResult{T0, T1}"/> struct.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    public PairResult(T0 item0, T1 item1)
    {
        this.Item0 = item0;
        this.Item1 = item1;
    }

    /// <summary>
    /// Deconstructs the pair into its two items.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    public void Deconstruct(out T0 item0, out T1 item1)
    {
        item0 = this.Item0;
        item1 = this.Item1;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() =>
        $"({this.Item0}, {this.Item1})";
}

/// <summary>
/// Represents a triple of three items.
/// </summary>
/// <typeparam name="T0">The type of the first item.</typeparam>
/// <typeparam name="T1">The type of the second item.</typeparam>
/// <typeparam name="T2">The type of the third item.</typeparam>
[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2>
{
    /// <summary>
    /// The first item.
    /// </summary>
    public readonly T0 Item0;

    /// <summary>
    /// The second item.
    /// </summary>
    public readonly T1 Item1;

    /// <summary>
    /// The third item.
    /// </summary>
    public readonly T2 Item2;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairResult{T0, T1, T2}"/> struct.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    public PairResult(T0 item0, T1 item1, T2 item2)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
    }

    /// <summary>
    /// Deconstructs the triple into its three items.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    public void Deconstruct(out T0 item0, out T1 item1, out T2 item2)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() =>
        $"({this.Item0}, {this.Item1}, {this.Item2})";
}

/// <summary>
/// Represents a quadruple of four items.
/// </summary>
/// <typeparam name="T0">The type of the first item.</typeparam>
/// <typeparam name="T1">The type of the second item.</typeparam>
/// <typeparam name="T2">The type of the third item.</typeparam>
/// <typeparam name="T3">The type of the fourth item.</typeparam>
[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2, T3>
{
    /// <summary>
    /// The first item.
    /// </summary>
    public readonly T0 Item0;

    /// <summary>
    /// The second item.
    /// </summary>
    public readonly T1 Item1;

    /// <summary>
    /// The third item.
    /// </summary>
    public readonly T2 Item2;

    /// <summary>
    /// The fourth item.
    /// </summary>
    public readonly T3 Item3;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairResult{T0, T1, T2, T3}"/> struct.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    /// <param name="item3">The fourth item.</param>
    public PairResult(
        T0 item0, T1 item1, T2 item2, T3 item3)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
    }

    /// <summary>
    /// Deconstructs the quadruple into its four items.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    /// <param name="item3">The fourth item.</param>
    public void Deconstruct(
        out T0 item0, out T1 item1, out T2 item2, out T3 item3)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() =>
        $"({this.Item0}, {this.Item1}, {this.Item2}, {this.Item3})";
}

/// <summary>
/// Represents a quintuple of five items.
/// </summary>
/// <typeparam name="T0">The type of the first item.</typeparam>
/// <typeparam name="T1">The type of the second item.</typeparam>
/// <typeparam name="T2">The type of the third item.</typeparam>
/// <typeparam name="T3">The type of the fourth item.</typeparam>
/// <typeparam name="T4">The type of the fifth item.</typeparam>
[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2, T3, T4>
{
    /// <summary>
    /// The first item.
    /// </summary>
    public readonly T0 Item0;

    /// <summary>
    /// The second item.
    /// </summary>
    public readonly T1 Item1;

    /// <summary>
    /// The third item.
    /// </summary>
    public readonly T2 Item2;

    /// <summary>
    /// The fourth item.
    /// </summary>
    public readonly T3 Item3;

    /// <summary>
    /// The fifth item.
    /// </summary>
    public readonly T4 Item4;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairResult{T0, T1, T2, T3, T4}"/> struct.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    /// <param name="item3">The fourth item.</param>
    /// <param name="item4">The fifth item.</param>
    public PairResult(
        T0 item0, T1 item1, T2 item2, T3 item3, T4 item4)
    {
        this.Item0 = item0;
        this.Item1 = item1;
        this.Item2 = item2;
        this.Item3 = item3;
        this.Item4 = item4;
    }

    /// <summary>
    /// Deconstructs the quintuple into its five items.
    /// </summary>
    /// <param name="item0">The first item.</param>
    /// <param name="item1">The second item.</param>
    /// <param name="item2">The third item.</param>
    /// <param name="item3">The fourth item.</param>
    /// <param name="item4">The fifth item.</param>
    public void Deconstruct(
        out T0 item0, out T1 item1, out T2 item2, out T3 item3, out T4 item4)
    {
        item0 = this.Item0;
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
        item4 = this.Item4;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() =>
        $"({this.Item0}, {this.Item1}, {this.Item2}, {this.Item3}, {this.Item4})";
}
