////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.IO.Compression;

namespace GitReader;

public sealed class RepositoryTestsSetUp
{
    private static readonly string basePath =
        Path.Combine("tests", $"{DateTime.Now:yyyyMMdd_HHmmss}");

    public static string GetBasePath(string artifact) =>
        Path.Combine(basePath, artifact);

    static RepositoryTestsSetUp()
    {
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
