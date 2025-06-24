////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.Structures;

/// <summary>
/// Provides extension methods for creating structured repository instances.
/// </summary>
public static class RepositoryFactoryExtension
{
    /// <summary>
    /// Opens a structured repository at the specified path using the default file system.
    /// </summary>
    /// <param name="_">The repository factory instance.</param>
    /// <param name="path">The path to the repository.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a StructuredRepository instance.</returns>
    public static Task<StructuredRepository> OpenStructureAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenStructuredAsync(
            path, new StandardFileSystem(65536), LooseConcurrentScope.Default, ct);

    /// <summary>
    /// Opens a structured repository at the specified path using a custom file system.
    /// </summary>
    /// <param name="_">The repository factory instance.</param>
    /// <param name="path">The path to the repository.</param>
    /// <param name="fileSystem">The file system implementation to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a StructuredRepository instance.</returns>
    public static Task<StructuredRepository> OpenStructureAsync(
        this RepositoryFactory _,
        string path, IFileSystem fileSystem, CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenStructuredAsync(
            path, fileSystem, LooseConcurrentScope.Default, ct);

    /// <summary>
    /// Opens a structured repository at the specified path using a custom file system.
    /// </summary>
    /// <param name="_">The repository factory instance.</param>
    /// <param name="path">The path to the repository.</param>
    /// <param name="fileSystem">The file system implementation to use.</param>
    /// <param name="concurrentScope">Concurrent scope.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns a StructuredRepository instance.</returns>
    public static Task<StructuredRepository> OpenStructureAsync(
        this RepositoryFactory _,
        string path, IFileSystem fileSystem, IConcurrentScope concurrentScope, CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenStructuredAsync(
            path, fileSystem, concurrentScope, ct);
}
