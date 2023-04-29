////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader

open System
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module internal Utilities =

    let unwrapOption(v: 'T option) =
        match v with
        | None -> Unchecked.defaultof<'T>
        | Some v -> v
    let inline unwrapCT(ct: CancellationToken option) =
        match ct with
        | Some ct -> ct
        | None -> CancellationToken()

    let inline asAsync(task: Task<Nullable<'T>>) = async {
        let! result = task |> Async.AwaitTask
        if result.HasValue then
            return Some result.Value
        else
            return None
    }
