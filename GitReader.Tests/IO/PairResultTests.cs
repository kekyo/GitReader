////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using NUnit.Framework;
using GitReader.IO;

namespace GitReader.IO;

public sealed class PairResultTests
{
    [Test]
    public void PairResult_TwoItems_ToString()
    {
        var pair = new PairResult<string, int>("hello", 42);
        var result = pair.ToString();
        
        Assert.That(result, Is.EqualTo("(hello, 42)"));
    }

    [Test]
    public void PairResult_TwoItems_WithNulls_ToString()
    {
        var pair = new PairResult<string?, int?>(null, null);
        var result = pair.ToString();
        
        Assert.That(result, Is.EqualTo("(, )"));
    }

    [Test]
    public void PairResult_ThreeItems_ToString()
    {
        var triple = new PairResult<string, int, bool>("test", 123, true);
        var result = triple.ToString();
        
        Assert.That(result, Is.EqualTo("(test, 123, True)"));
    }

    [Test]
    public void PairResult_FourItems_ToString()
    {
        var quad = new PairResult<string, int, bool, double>("data", 456, false, 3.14);
        var result = quad.ToString();
        
        Assert.That(result, Is.EqualTo("(data, 456, False, 3.14)"));
    }

    [Test]
    public void PairResult_FiveItems_ToString()
    {
        var quint = new PairResult<string, int, bool, double, char>("item", 789, true, 2.71, 'X');
        var result = quint.ToString();
        
        Assert.That(result, Is.EqualTo("(item, 789, True, 2.71, X)"));
    }

    [Test]
    public void PairResult_Deconstruct_TwoItems()
    {
        var pair = new PairResult<string, int>("hello", 42);
        var (item0, item1) = pair;
        
        Assert.That(item0, Is.EqualTo("hello"));
        Assert.That(item1, Is.EqualTo(42));
    }

    [Test]
    public void PairResult_Deconstruct_ThreeItems()
    {
        var triple = new PairResult<string, int, bool>("test", 123, true);
        var (item0, item1, item2) = triple;
        
        Assert.That(item0, Is.EqualTo("test"));
        Assert.That(item1, Is.EqualTo(123));
        Assert.That(item2, Is.EqualTo(true));
    }

    [Test]
    public void PairResult_Deconstruct_FourItems()
    {
        var quad = new PairResult<string, int, bool, double>("data", 456, false, 3.14);
        var (item0, item1, item2, item3) = quad;
        
        Assert.That(item0, Is.EqualTo("data"));
        Assert.That(item1, Is.EqualTo(456));
        Assert.That(item2, Is.EqualTo(false));
        Assert.That(item3, Is.EqualTo(3.14));
    }

    [Test]
    public void PairResult_Deconstruct_FiveItems()
    {
        var quint = new PairResult<string, int, bool, double, char>("item", 789, true, 2.71, 'X');
        var (item0, item1, item2, item3, item4) = quint;
        
        Assert.That(item0, Is.EqualTo("item"));
        Assert.That(item1, Is.EqualTo(789));
        Assert.That(item2, Is.EqualTo(true));
        Assert.That(item3, Is.EqualTo(2.71));
        Assert.That(item4, Is.EqualTo('X'));
    }
} 