////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace GitReader.Internal;

public sealed class RepositoryAccessorTests
{
    [TestCase(1)]
    [TestCase(2)]
    public async Task DetectLocalRepositoryPath(int depth)
    {
        var basePath = Path.GetFullPath(
            RepositoryTestsSetUp.GetBasePath(
                $"DetectLocalRepositoryPath_{depth}"));
        var innerPath = Path.Combine(
            basePath,
            Path.Combine(Enumerable.Range(0, depth).Select(_ => "inner").ToArray()));

        Directory.CreateDirectory(innerPath);

        ZipFile.ExtractToDirectory(
            Path.Combine("artifacts", "test1.zip"),
            basePath);

        var actual = await RepositoryAccessor.DetectLocalRepositoryPathAsync(
            innerPath,
            new StandardFileSystem(65536),
            default);

        Assert.AreEqual(Path.Combine(basePath, ".git"), actual.GitPath);
        Assert.AreEqual(0, actual.AlternativePaths.Length);
    }

    [Test]
    public async Task DetectLocalRepositoryPathFromDotGitFile()
    {
        var basePath = Path.GetFullPath(
            RepositoryTestsSetUp.GetBasePath(
                $"DetectLocalRepositoryPathFromDotGitFile"));

        ZipFile.ExtractToDirectory(
            Path.Combine("artifacts", "test5.zip"),
            basePath);

        var innerPath = Path.Combine(basePath, "GitReader");

        var actual = await RepositoryAccessor.DetectLocalRepositoryPathAsync(
            innerPath,
            new StandardFileSystem(65536),
            default);

        Assert.AreEqual(Path.Combine(basePath, ".git", "modules", "GitReader"), actual.GitPath);
        Assert.AreEqual(0, actual.AlternativePaths.Length);
    }

    [Test]
    public async Task DetectLocalRepositoryPathFromWorkTree()
    {
        var basePath = Path.GetFullPath(
            RepositoryTestsSetUp.GetBasePath(
                $"DetectLocalRepositoryPathFromWorkTree"));

        ZipFile.ExtractToDirectory(
            Path.Combine("artifacts", "test6.zip"),
            basePath);

        var innerPath = Path.Combine(basePath, "worktree");

        var actual = await RepositoryAccessor.DetectLocalRepositoryPathAsync(
            innerPath,
            new StandardFileSystem(65536),
            default);

        Assert.AreEqual(Path.Combine(basePath, "root", ".git"), actual.GitPath);
        Assert.AreEqual(new[] { Path.Combine(basePath, "root", ".git", "worktrees", "worktree") }, actual.AlternativePaths);
    }
}
