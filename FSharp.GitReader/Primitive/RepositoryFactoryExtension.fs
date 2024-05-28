////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open GitReader
open GitReader.IO
open System.Threading

[<AutoOpen>]
module public RepositoryFactoryExtension =

    type RepositoryFactory with
        member _.openPrimitive(path: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.OpenPrimitiveAsync(
                path, new StandardFileSystem(65536), unwrapCT ct) |> Async.AwaitTask
        member _.openPrimitive(path: string, fileSystem: IFileSystem, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.OpenPrimitiveAsync(
                path, fileSystem, unwrapCT ct) |> Async.AwaitTask
