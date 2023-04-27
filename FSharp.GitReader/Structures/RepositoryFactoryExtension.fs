////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open System.Threading

[<AutoOpen>]
module public RepositoryFactoryExtension =

    type RepositoryFactory with
        member _.openStructured(path: string, ?ct: CancellationToken) =
            RepositoryFacade.OpenStructuredAsync(
                path, unwrapCT ct) |> Async.AwaitTask
