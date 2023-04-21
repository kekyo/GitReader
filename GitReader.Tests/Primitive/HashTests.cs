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

namespace GitReader.Primitive;

public sealed class HashTests
{
    [Test]
    public void Create()
    {
        var hashCode = new byte[]
        {
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef,
            0x01, 0x23, 0x45, 0x67,
        };
        var hash = Hash.Create(hashCode);

        Assert.AreEqual(hashCode, hash.HashCode);
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

        var hash = Hash.Parse(hashCodeString);

        Assert.AreEqual(hashCode, hash.HashCode);
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

        var hash1 = Hash.Create(hashCode1);
        var hash2 = Hash.Create(hashCode2);

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

        var hash1 = Hash.Create(hashCode1);
        var hash2 = Hash.Create(hashCode2);

        Assert.IsFalse(hash1.Equals(hash2));
    }
}
