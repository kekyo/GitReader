////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using NUnit.Framework;
using System;

namespace GitReader;

public sealed class SignatureTests
{
    [Test]
    public void Constructor1()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);
        var sig = new Signature("Foo Bar", now);

        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual(now, sig.Date);
        Assert.IsNull(sig.MailAddress);
    }

    [Test]
    public void Constructor2()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);
        var sig = new Signature("Foo Bar", "foo@bar.com", now);

        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual("foo@bar.com", sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }

    [Test]
    public void Parse1()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var sig = Signature.Parse(
            $"Foo Bar {now.ToUnixTimeSeconds()} {now.Offset:hhmm}");

        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual(now, sig.Date);
        Assert.IsNull(sig.MailAddress);
    }

    [Test]
    public void Parse2()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var sig = Signature.Parse(
            $"Foo Bar <foobar@baz.com> {now.ToUnixTimeSeconds()} {now.Offset:hhmm}");

        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual("foobar@baz.com", sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }

    [Test]
    public void Parse3()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var sig = Signature.Parse(
            $"FooBar <foobar@baz.com> {now.ToUnixTimeSeconds()} {now.Offset:hhmm}");

        Assert.AreEqual("FooBar", sig.Name);
        Assert.AreEqual("foobar@baz.com", sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }

    [Test]
    public void Parse4()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var sig = Signature.Parse(
            $"FooBar {now.ToUnixTimeSeconds()} {now.Offset:hhmm}");

        Assert.AreEqual("FooBar", sig.Name);
        Assert.AreEqual(now, sig.Date);
        Assert.IsNull(sig.MailAddress);
    }

    [Test]
    public void TryParse1()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"Foo Bar {now.ToUnixTimeSeconds()} {now.Offset:hhmm}", out var sig);

        Assert.IsTrue(r);
        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual(now, sig.Date);
        Assert.IsNull(sig.MailAddress);
    }

    [Test]
    public void TryParse2()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"Foo Bar <foobar@baz.com> {now.ToUnixTimeSeconds()} {now.Offset:hhmm}", out var sig);

        Assert.IsTrue(r);
        Assert.AreEqual("Foo Bar", sig.Name);
        Assert.AreEqual("foobar@baz.com", sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }

    [Test]
    public void TryParse3()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"FooBar <foobar@baz.com> {now.ToUnixTimeSeconds()} {now.Offset:hhmm}", out var sig);

        Assert.IsTrue(r);
        Assert.AreEqual("FooBar", sig.Name);
        Assert.AreEqual("foobar@baz.com", sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }

    [Test]
    public void TryParse4()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"FooBar {now.ToUnixTimeSeconds()} {now.Offset:hhmm}", out var sig);

        Assert.IsTrue(r);
        Assert.AreEqual("FooBar", sig.Name);
        Assert.AreEqual(now, sig.Date);
        Assert.IsNull(sig.MailAddress);
    }

    [Test]
    public void TryParse5()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"Foo Bar 115qaf {now.Offset:hhmm}", out var _);

        Assert.IsFalse(r);
    }

    [Test]
    public void TryParse6()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"Foo Bar {now.ToUnixTimeSeconds()} 1w23", out var _);

        Assert.IsFalse(r);
    }

    [Test]
    public void TryParse7()
    {
        var now = Utilities.TruncateMilliseconds(DateTimeOffset.Now);

        var r = Signature.TryParse(
            $"Foo Bar <foobar@baz.com] {now.ToUnixTimeSeconds()} {now.Offset:hhmm}", out var sig);

        Assert.IsTrue(r);
        Assert.AreEqual("Foo Bar <foobar@baz.com]", sig.Name);
        Assert.IsNull(sig.MailAddress);
        Assert.AreEqual(now, sig.Date);
    }
}
