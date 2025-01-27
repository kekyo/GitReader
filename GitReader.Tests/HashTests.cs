////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using System;
using System.Linq;

namespace GitReader;

public sealed class HashTests
{
    [Test]
    public void Constructor1()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };

        var actual = new Hash(hashCode);

        Assert.AreEqual(hashCode, actual.HashCode);
    }

    [Test]
    public void Constructor2()
    {
        var hashString = "1205dc34ce48bda28fc543daaf9525a9bb6e6d10";
        var hashCode = Enumerable.Range(0, hashString.Length / 2).
            Select(index => Convert.ToByte(hashString.Substring(index * 2, 2), 16)).
            ToArray();

        var actual = new Hash(hashString);

        Assert.AreEqual(hashCode, actual.HashCode);
    }

    [Test]
    public void Parse()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };

        var hashCodeString = BitConverter.ToString(hashCode).Replace("-", "");

        var actual = Hash.Parse(hashCodeString);

        Assert.AreEqual(hashCode, actual.HashCode);
    }

    [Test]
    public void TryParse1()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };

        var hashCodeString = BitConverter.ToString(hashCode).Replace("-", "");

        var r = Hash.TryParse(hashCodeString, out var actual);

        Assert.IsTrue(r);
        Assert.AreEqual(hashCode, actual.HashCode);
    }

    [Test]
    public void TryParse2()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45,
        };

        var hashCodeString = BitConverter.ToString(hashCode).Replace("-", "");

        var r = Hash.TryParse(hashCodeString, out var _);

        Assert.IsFalse(r);
    }

    [Test]
    public void TryParse3()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89,
        };

        var hashCodeString = BitConverter.ToString(hashCode).Replace("-", "");

        var r = Hash.TryParse(hashCodeString, out var _);

        Assert.IsFalse(r);
    }

    [Test]
    public void TryParse4()
    {
        var hashString = "1205dc34ce48bda28fx543daaf9525a9bb6e6d10";
        var r = Hash.TryParse(hashString, out var _);

        Assert.IsFalse(r);
    }

    [Test]
    public void Equals1()
    {
        var hashCode1 = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };
        var hashCode2 = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };

        Hash hash1 = hashCode1;
        Hash hash2 = hashCode2;

        Assert.IsTrue(hash1.Equals(hash2));
    }

    [Test]
    public void Equals2()
    {
        var hashCode1 = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };
        var hashCode2 = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x6a, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };

        Hash hash1 = hashCode1;
        Hash hash2 = hashCode2;

        Assert.IsFalse(hash1.Equals(hash2));
    }
}
