////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections;

public static class ArrayExtension
{
    public static ReadOnlyArray<TValue> AsReadOnly<TValue>(
        this TValue[] array) =>
        new(array);

    public static bool Contains<TValue>(
        this ReadOnlyArray<TValue> array,
        TValue item) =>
        array.Contains(item);

    public static int IndexOf<TValue>(
        this ReadOnlyArray<TValue> array,
        TValue item) =>
        array.IndexOf(item);

    public static TValue[] Clone<TValue>(
        this ReadOnlyArray<TValue> array) =>
        array.Clone();
}
