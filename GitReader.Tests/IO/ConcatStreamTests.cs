////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitReader.IO;

public sealed class ConcatStreamTests
{
    [Test]
    public void Concat1Stream1()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public void Concat1Stream2()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[4];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());
    }

    [Test]
    public void Concat1Stream3()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[2];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public void Concat1Stream4()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

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
    public void Concat2Stream1()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[6];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public void Concat2Stream2()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[7];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b.Take(6).ToArray());
    }

    [Test]
    public void Concat2Stream31()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[2];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public void Concat2Stream32()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[5];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(5, read);
        Assert.AreEqual(expected.Take(5).ToArray(), b);
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task Concat1Stream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public async Task Concat1Stream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[4];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());
    }

    [Test]
    public async Task Concat1Stream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

        var b = new byte[2];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public async Task Concat1Stream4Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var s = new ConcatStream(
            new MemoryStream(expected));

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
    public async Task Concat2Stream1Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[6];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public async Task Concat2Stream2Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[7];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b.Take(6).ToArray());
    }

    [Test]
    public async Task Concat2Stream31Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[2];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public async Task Concat2Stream32Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[5];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(5, read);
        Assert.AreEqual(expected.Take(5).ToArray(), b);
    }

    [Test]
    public async Task Concat2Stream4Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[4];
        var read1 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(4, read1);
        Assert.AreEqual(expected.Take(4).ToArray(), b);

        var read2 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read2);
        Assert.AreEqual(expected.Skip(4).ToArray(), b.Take(2).ToArray());
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task Concat2ValueTaskStream1Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[6];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b);
    }

    [Test]
    public async Task Concat2ValueTaskStream2Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[7];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(6, read);
        Assert.AreEqual(expected, b.Take(6).ToArray());
    }

    [Test]
    public async Task Concat2ValueTaskStream31Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[2];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);
    }

    [Test]
    public async Task Concat2ValueTaskStream32Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[5];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(5, read);
        Assert.AreEqual(expected.Take(5).ToArray(), b);
    }

    [Test]
    public async Task Concat2ValueTaskStream4Async()
    {
        var e1 = new byte[] { 0, 1, 2 };
        var e2 = new byte[] { 3, 4, 5 };
        var expected = e1.Concat(e2).ToArray();

        var s = new ConcatStream(
            new MemoryStream(e1),
            new MemoryStream(e2));

        var b = new byte[4];
        var read1 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(4, read1);
        Assert.AreEqual(expected.Take(4).ToArray(), b);

        var read2 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read2);
        Assert.AreEqual(expected.Skip(4).ToArray(), b.Take(2).ToArray());
    }
}
