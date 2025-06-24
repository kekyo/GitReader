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

[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1>
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

[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2>
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

[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2, T3>
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

[EditorBrowsable((EditorBrowsableState.Never))]
[DebuggerStepThrough]
public readonly struct PairResult<T0, T1, T2, T3, T4>
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
