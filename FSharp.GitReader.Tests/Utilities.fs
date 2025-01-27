////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
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
open GitReader.Collections

[<Sealed>]
type private ByteDataConverter() =
    inherit WriteOnlyJsonConverter<byte[]>()
    override _.Write(writer: VerifyJsonWriter, data: byte[]) =
        writer.WriteValue(
            BitConverter.ToString(data).Replace("-", "").ToLowerInvariant())

[<Sealed>]
type private BranchArrayConverter() =
    inherit WriteOnlyJsonConverter<ReadOnlyArray<Structures.Branch>>()
    override _.Write(writer: VerifyJsonWriter, branches: ReadOnlyArray<Structures.Branch>) =
        // Avoid infinite reference by Branch.Head.
        writer.WriteStartArray()
        for branch in branches do
          writer.WriteStartObject()
          writer.WritePropertyName("Name")
          writer.WriteValue(branch.Name)
          writer.WritePropertyName("IsRemote")
          writer.WriteValue(branch.IsRemote)
          writer.WriteEndObject()
        writer.WriteEndArray()

[<Sealed; AbstractClass>]
type public RepositoryTestsSetUp() =
    static let mutable basePath = ""
    static do
        basePath <- Path.Combine("tests", $"{DateTime.Now:yyyyMMdd_HHmmss}")
        VerifierSettings.DontScrubDateTimes()
        VerifierSettings.AddExtraSettings(fun setting ->
            setting.Converters.Add(ByteDataConverter())
            setting.Converters.Add(BranchArrayConverter()))
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
