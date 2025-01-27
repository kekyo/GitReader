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

namespace GitReader.Primitive;

public static class RepositoryFactoryExtension
{
    public static Task<PrimitiveRepository> OpenPrimitiveAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.OpenPrimitiveAsync(
            path, new StandardFileSystem(65536), ct);

    public static Task<PrimitiveRepository> OpenPrimitiveAsync(
        this RepositoryFactory _,
        string path, IFileSystem fileSystem, CancellationToken ct = default) =>
        PrimitiveRepositoryFacade.OpenPrimitiveAsync(
            path, fileSystem, ct);
}
