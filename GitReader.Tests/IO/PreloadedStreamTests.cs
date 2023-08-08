////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitReader.IO;

public sealed class PreloadedStreamTests
{
    [Test]
    public void PreloadedStream1()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public void PreloadedStream2()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[4];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());
    }

    [Test]
    public void PreloadedStream3()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public void PreloadedStream4()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 1, expected.Length);

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Skip(1).ToArray(), b.Take(2).ToArray());
    }

    [Test]
    public void PreloadedStream5()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read1 = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        var read2 = s.Read(b, 0, b.Length);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task PreloadedStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public async Task PreloadedStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[4];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());
    }

    [Test]
    public async Task PreloadedStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public async Task PreloadedStream4Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 1, expected.Length);

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Skip(1).ToArray(), b.Take(2).ToArray());
    }

    [Test]
    public async Task PreloadedStream5Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read1 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        var read2 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task PreloadedValueTaskStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[3];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public async Task PreloadedValueTaskStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[4];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());
    }

    [Test]
    public async Task PreloadedValueTaskStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public async Task PreloadedValueTaskStream4Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 1, expected.Length);

        var b = new byte[3];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Skip(1).ToArray(), b.Take(2).ToArray());
    }

    [Test]
    public async Task PreloadedValueTaskStream5Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new PreloadedStream(new(null, expected), 0, expected.Length);

        var b = new byte[2];
        var read1 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        var read2 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());
    }
}
