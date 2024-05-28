////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

public static class RepositoryFactoryExtension
{
    public static Task<StructuredRepository> OpenStructureAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenStructuredAsync(path, new StandardFileSystem(65536), ct);

    public static Task<StructuredRepository> OpenStructureAsync(
        this RepositoryFactory _,
        string path, IFileSystem fileSystem, CancellationToken ct = default) =>
        StructuredRepositoryFacade.OpenStructuredAsync(path, fileSystem,ct);
}
