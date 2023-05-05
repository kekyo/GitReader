////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitReader.Internal;

public sealed class RangedStreamTests
{
    [Test]
    public void RangedStream1()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }

    [Test]
    public void RangedStream2()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), 2);

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b.Take(2).ToArray());

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(2, s.Position);
    }

    [Test]
    public void RangedStream3()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[2];
        var read1 = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(2, s.Position);

        var read2 = s.Read(b, 0, b.Length);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task RangedStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }

    [Test]
    public async Task RangedStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), 2);

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b.Take(2).ToArray());

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(2, s.Position);
    }

    [Test]
    public async Task RangedStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[2];
        var read1 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(2, s.Position);

        var read2 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task RangedValueTaskStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }

    [Test]
    public async Task RangedValueTaskStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), 2);

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[3];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b.Take(2).ToArray());

        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(2, s.Position);
    }

    [Test]
    public async Task RangedValueTaskStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new RangedStream(
            new MemoryStream(expected), expected.Length);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(0, s.Position);

        var b = new byte[2];
        var read1 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(2, s.Position);

        var read2 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(3, s.Length);
        Assert.AreEqual(3, s.Position);
    }
}
