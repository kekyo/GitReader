////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using GitReader.IO;

namespace GitReader.Primitive;

/// <summary>
/// Represents a primitive Git repository that provides low-level access to Git objects and operations.
/// </summary>
public sealed class PrimitiveRepository : Repository
{
    /// <summary>
    /// Initializes a new instance of the PrimitiveRepository class.
    /// </summary>
    /// <param name="gitPath">The path to the Git repository.</param>
    /// <param name="alternativePaths">Alternative paths to try when accessing the repository.</param>
    /// <param name="fileSystem">The file system implementation to use.</param>
    /// <param name="concurrentScope">Concurrent scope.</param>
    internal PrimitiveRepository(
        string gitPath,
        string[] alternativePaths,
        IFileSystem fileSystem,
        IConcurrentScope concurrentScope) :
        base(gitPath, alternativePaths, fileSystem, concurrentScope)
    {
    }
}
