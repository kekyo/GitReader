////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GitReader.IO;

public sealed class MemoizedStreamTests
{
    private readonly BufferPool pool = new();

    [Test]
    public void MemoizedStream1()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[3];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public void MemoizedStream2()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());
    }

    [Test]
    public void MemoizedStream3()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[4];
        var read = s.Read(b, 0, b.Length);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public void MemoizedStream4()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        var read2 = s.Read(b, 0, b.Length);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public void MemoizedStream5()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        s.Seek(1, SeekOrigin.Begin);

        Assert.AreEqual(parent.Length, s.Length);

        Assert.AreEqual(2, parent.Position);
        Assert.AreEqual(1, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(1, memoized.Position);

        var read2 = s.Read(b, 0, b.Length);

        Assert.AreEqual(2, read2);
        Assert.AreEqual(expected.Skip(1).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected.Take(3).ToArray(), memoized.ToArray());
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task MemoizedStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[3];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());
    }

    [Test]
    public async Task MemoizedStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[4];
        var read = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedStream4Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        var read2 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedStream5Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        s.Seek(1, SeekOrigin.Begin);

        Assert.AreEqual(parent.Length, s.Length);

        Assert.AreEqual(2, parent.Position);
        Assert.AreEqual(1, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(1, memoized.Position);

        var read2 = await s.ReadAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read2);
        Assert.AreEqual(expected.Skip(1).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected.Take(3).ToArray(), memoized.ToArray());
    }

    //////////////////////////////////////////////////////////////

    [Test]
    public async Task MemoizedValueTaskStream1Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[3];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedValueTaskStream2Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());
    }

    [Test]
    public async Task MemoizedValueTaskStream3Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[4];
        var read = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(3, read);
        Assert.AreEqual(expected, b.Take(3).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedValueTaskStream4Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        var read2 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(1, read2);
        Assert.AreEqual(expected.Skip(2).ToArray(), b.Take(1).ToArray());

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected, memoized.ToArray());
    }

    [Test]
    public async Task MemoizedValueTaskStream5Async()
    {
        var expected = new byte[] { 0, 1, 2 };

        var parent = new MemoryStream(expected);
        var memoized = new MemoryStream();
        var s = new MemoizedStream(parent, parent.Length, null, memoized, this.pool);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(0, memoized.Length);
        Assert.AreEqual(0, memoized.Position);

        var b = new byte[2];
        var read1 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read1);
        Assert.AreEqual(expected.Take(2).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(2, memoized.Position);

        Assert.AreEqual(expected.Take(2).ToArray(), memoized.ToArray());

        s.Seek(1, SeekOrigin.Begin);

        Assert.AreEqual(parent.Length, s.Length);

        Assert.AreEqual(2, parent.Position);
        Assert.AreEqual(1, s.Position);

        Assert.AreEqual(2, memoized.Length);
        Assert.AreEqual(1, memoized.Position);

        var read2 = await s.ReadValueTaskAsync(b, 0, b.Length, default);

        Assert.AreEqual(2, read2);
        Assert.AreEqual(expected.Skip(1).ToArray(), b);

        Assert.AreEqual(parent.Length, s.Length);
        Assert.AreEqual(parent.Position, s.Position);

        Assert.AreEqual(3, memoized.Length);
        Assert.AreEqual(3, memoized.Position);

        Assert.AreEqual(expected.Take(3).ToArray(), memoized.ToArray());
    }
}
