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
open System.IO
open System.IO.Compression
open System.Threading.Tasks
open VerifyNUnit
open VerifyTests

[<Sealed>]
type private ByteDataConverter() =
    inherit WriteOnlyJsonConverter<byte[]>()
    override _.Write(writer: VerifyJsonWriter, data: byte[]) =
        writer.WriteValue(
            BitConverter.ToString(data).Replace("-", "").ToLowerInvariant())

[<Sealed; AbstractClass>]
type public RepositoryTestsSetUp() =
    static let mutable basePath = ""
    static do
        basePath <- Path.Combine("tests", $"{DateTime.Now:yyyyMMdd_HHmmss}")
        VerifierSettings.DontScrubDateTimes()
        VerifierSettings.AddExtraSettings(fun setting ->
            setting.Converters.Add(ByteDataConverter()))
        if not (Directory.Exists basePath) then
            try
                Directory.CreateDirectory(basePath) |> ignore
            with
            | _ -> ()
        ZipFile.ExtractToDirectory(
            "artifacts/test1.zip", basePath)
    static member BasePath = basePath

[<AutoOpen>]
module public Utilities =
    let verify(v: obj) = async {
        let! _ = Verifier.Verify(v).ToTask() |> Async.AwaitTask
        return ()
    }
    let unwrapOption(v: 'T option) =
        match v with
        | None -> Unchecked.defaultof<'T>
        | Some v -> v
    let unwrapOptionAsy(asy: Async<'T option>) = async {
        let! v = asy
        return unwrapOption v
    }
