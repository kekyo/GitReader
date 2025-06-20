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
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Threading
open System.Threading.Tasks

[<Sealed; AbstractClass>]
type public RepositoryTestsSetUp() =
    static let mutable basePath = ""
    static do
        basePath <- Path.Combine("tests", $"{DateTime.Now:yyyyMMdd_HHmmss}")

        if not (Directory.Exists basePath) then
            try
                Directory.CreateDirectory(basePath) |> ignore
            with
            | _ -> ()
        Directory.EnumerateFiles(
            "artifacts", "*.zip", SearchOption.AllDirectories)
            |> Seq.iter (fun path ->
                let baseName = Path.GetFileNameWithoutExtension path
                ZipFile.ExtractToDirectory(
                    path, RepositoryTestsSetUp.getBasePath baseName))
    static member getBasePath(artifact: string) =
        Path.Combine(basePath, artifact)

[<AutoOpen>]
module public Utilities =

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
    type ValueTask<'T> with
        member public task.asAsync() =
            task.AsTask() |> Async.AwaitTask
#endif

    let unwrapOption(v: 'T option) =
        match v with
        | None -> Unchecked.defaultof<'T>
        | Some v -> v
    let unwrapOptionAsy(asy: Async<'T option>) = async {
        let! v = asy
        return unwrapOption v
    }
    let runGitCommandAsync(workingDirectory: string, arguments: string) = async {
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "git"
        startInfo.Arguments <- arguments
        startInfo.WorkingDirectory <- workingDirectory
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false
        startInfo.CreateNoWindow <- true

        use proc = new Process()
        proc.StartInfo <- startInfo

        let r = proc.Start()
        if (r = false) then
            raise (InvalidOperationException())

#if NETFRAMEWORK
        do! Task.Run(fun () -> proc.WaitForExit()).asAsync()
#else
        do! proc.WaitForExitAsync().asAsync()
#endif

        if proc.ExitCode <> 0 then
            let! error = proc.StandardError.ReadToEndAsync().asAsync()
            raise (InvalidOperationException($"Git command failed: git {arguments}\nError: {error}"))
    }

type TestUtilities =
    static member WriteAllTextAsync(path: string, contents: string) =
#if NETFRAMEWORK
        Task.Run(fun () -> File.WriteAllText(path, contents))
#else
        File.WriteAllTextAsync(path, contents)
#endif
