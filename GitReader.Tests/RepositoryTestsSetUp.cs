////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using System;
using System.IO;
using System.IO.Compression;
using VerifyTests;

namespace GitReader;

public sealed class RepositoryTestsSetUp
{
    private sealed class ByteDataConverter :
        WriteOnlyJsonConverter<byte[]>
    {
        public override void Write(VerifyJsonWriter writer, byte[] data) =>
            writer.WriteValue(
                BitConverter.ToString(data).Replace("-", string.Empty).ToLowerInvariant());
    }

    private sealed class BranchArrayConverter :
        WriteOnlyJsonConverter<ReadOnlyArray<Structures.Branch>>
    {
        public override void Write(
            VerifyJsonWriter writer, ReadOnlyArray<Structures.Branch> branches)
        {
            // Avoid infinite reference by Branch.Head.
            writer.WriteStartArray();
            foreach (var branch in branches)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Name");
                writer.WriteValue(branch.Name);
                writer.WritePropertyName("IsRemote");
                writer.WriteValue(branch.IsRemote);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }

    private static readonly string basePath =
        Path.Combine("tests", $"{DateTime.Now:yyyyMMdd_HHmmss}");

    public static string GetBasePath(string artifact) =>
        Path.Combine(basePath, artifact);

    static RepositoryTestsSetUp()
    {
        VerifierSettings.DontScrubDateTimes();
        VerifierSettings.AddExtraSettings(setting =>
        {
            setting.Converters.Add(new ByteDataConverter());
            setting.Converters.Add(new BranchArrayConverter());
        });

        if (!Directory.Exists(basePath))
        {
            try
            {
                Directory.CreateDirectory(basePath);
            }
            catch
            {
            }

            foreach (var path in Directory.EnumerateFiles("artifacts", "*.zip", SearchOption.AllDirectories))
            {
                var baseName = Path.GetFileNameWithoutExtension(path);
                ZipFile.ExtractToDirectory(path, GetBasePath(baseName));
            }
        }
    }
}
