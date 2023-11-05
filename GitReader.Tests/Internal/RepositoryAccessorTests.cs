////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GitReader.Internal;

public sealed class RepositoryAccessorTests
{
    [TestCase(1)]
    [TestCase(2)]
    public void DetectLocalRepositoryPath(int depth)
    {
        var basePath = Path.GetFullPath(
            RepositoryTestsSetUp.GetBasePath(
                $"DetectLocalRepositoryPath1_{depth}"));
        var innerPath = Path.Combine(
            basePath,
            Path.Combine(Enumerable.Range(0, depth).Select(_ => "inner").ToArray()));

        Directory.CreateDirectory(innerPath);

        ZipFile.ExtractToDirectory(
            Path.Combine("artifacts", "test1.zip"),
            basePath);

        var actual = RepositoryAccessor.DetectLocalRepositoryPath(innerPath);

        Assert.AreEqual(Path.Combine(basePath, ".git"), actual);
    }
}
