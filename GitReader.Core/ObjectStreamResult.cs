////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

namespace GitReader;

public readonly struct ObjectStreamResult : IDisposable
{
    public readonly Stream Stream;
    public readonly ObjectTypes Type;

    public ObjectStreamResult(
        Stream stream, ObjectTypes type)
    {
        this.Stream = stream;
        this.Type = type;
    }

    public void Dispose() =>
        this.Stream.Dispose();

    void IDisposable.Dispose() =>
        this.Stream.Dispose();

    public override string ToString() =>
        $"ObjectStream: Type={this.Type}";
}
