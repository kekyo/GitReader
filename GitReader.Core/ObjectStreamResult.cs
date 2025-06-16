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

/// <summary>
/// Represents a result containing a stream and object type for Git objects.
/// </summary>
public readonly struct ObjectStreamResult : IDisposable
{
    /// <summary>
    /// The stream containing the object data.
    /// </summary>
    public readonly Stream Stream;
    
    /// <summary>
    /// The type of the Git object.
    /// </summary>
    public readonly ObjectTypes Type;

    /// <summary>
    /// Initializes a new instance of the ObjectStreamResult struct.
    /// </summary>
    /// <param name="stream">The stream containing the object data.</param>
    /// <param name="type">The type of the Git object.</param>
    public ObjectStreamResult(
        Stream stream, ObjectTypes type)
    {
        this.Stream = stream;
        this.Type = type;
    }

    /// <summary>
    /// Disposes the underlying stream.
    /// </summary>
    public void Dispose() =>
        this.Stream.Dispose();

    void IDisposable.Dispose() =>
        this.Stream.Dispose();

    /// <summary>
    /// Returns a string representation of the object stream result.
    /// </summary>
    /// <returns>A string representation of the object stream result.</returns>
    public override string ToString() =>
        $"ObjectStream: Type={this.Type}";
}
