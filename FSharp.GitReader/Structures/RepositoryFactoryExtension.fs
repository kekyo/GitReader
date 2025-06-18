////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open GitReader.IO
open System.Threading

[<AutoOpen>]
module public RepositoryFactoryExtension =

    type RepositoryFactory with
        member _.openStructured(path: string, ?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenStructuredAsync(
                path, new StandardFileSystem(65536), unwrapCT ct).asAsync()
        member _.openStructured(path: string, fileSystem: IFileSystem, ?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenStructuredAsync(
                path, fileSystem, unwrapCT ct).asAsync()
