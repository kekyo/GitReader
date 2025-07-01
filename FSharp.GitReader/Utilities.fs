﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
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

    let inline unwrapOption(v: 'T option) =
        match v with
        | None -> Unchecked.defaultof<'T>
        | Some v -> v
    let inline unwrapCT(ct: CancellationToken option) =
        match ct with
        | Some ct -> ct
        | None -> CancellationToken()
    let inline wrapOption(v: 'T) =
        match v with
        | null -> None
        | _ -> Some v
    let inline wrapOptionV(v: Nullable<'T>) =
        match v.HasValue with
        | true -> Some v.Value
        | _ -> None

    let inline asOptionAsync(task: Task<Nullable<'T>>) = async {
        let! result = task |> Async.AwaitTask
        if result.HasValue then
            return Some result.Value
        else
            return None
    }

    type Task with
        member public task.asAsync() =
            task |> Async.AwaitTask
    type Task<'T> with
        member public task.asAsync() =
            task |> Async.AwaitTask

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP2_1_OR_GREATER
    type ValueTask with
        member public task.asAsync() =
            task.AsTask() |> Async.AwaitTask
#endif
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    type ValueTask<'T> with
        member public task.asAsync() =
            task.AsTask() |> Async.AwaitTask
#endif
