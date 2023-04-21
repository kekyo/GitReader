////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;
using System.Linq;
using GitReader.Internal;

namespace GitReader.Primitive;

public readonly struct Signature : IEquatable<Signature>
{
    public readonly string Name;
    public readonly string? MailAddress;
    public readonly DateTimeOffset Date;

    private Signature(
        string name, string? mailAddress,
        DateTimeOffset date)
    {
        this.Name = name;
        this.MailAddress = mailAddress;
        this.Date = Utilities.TruncateMilliseconds(date);
    }

    private string RawDate =>
            $"{this.Date.ToUnixTimeSeconds()} {this.Date.Offset:hh}{this.Date.Offset:mm}";

    public string RawFormat =>
        this.MailAddress is { } mailAddress ?
            $"{this.Name} <{mailAddress}> {this.RawDate}" :
            $"{this.Name} {this.RawDate}";

    public bool Equals(Signature rhs) =>
        this.Name == rhs.Name &&
        this.MailAddress == rhs.MailAddress &&
        this.Date == rhs.Date;

    bool IEquatable<Signature>.Equals(Signature rhs) =>
        Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is Signature rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Name.GetHashCode() ^
        (this.MailAddress?.GetHashCode() ?? 0) ^
        this.Date.GetHashCode();

    public override string ToString() =>
        this.MailAddress is { } mailAddress ?
            $"{this.Name} <{mailAddress}> {this.Date}" :
            $"{this.Name} {this.Date}";

    public void Deconstruct(
        out string name, out string? mailAddress, out DateTimeOffset date)
    {
        name = this.Name;
        mailAddress = this.MailAddress;
        date = this.Date;
    }

    public static Signature Create(
        string name,
        DateTimeOffset date) =>
        new(name, null, date);

    public static Signature Create(
        string name, string mailAddress,
        DateTimeOffset date) =>
        new(name, mailAddress, date);

    public static Signature Parse(string personString)
    {
        var elements = personString.Split(
            new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (elements.Length < 3)
        {
            throw new FormatException();
        }

        var dateIndex = elements.Length - 2;
        var unixTimeString = elements[dateIndex];
        var offsetString = elements[dateIndex + 1];

        if (offsetString.Length < 4)
        {
            throw new FormatException();
        }
        var offsetHourString = offsetString.Substring(0, offsetString.Length - 2);
        var offsetMinuteString = offsetString.Substring(offsetString.Length - 2, 2);

        if (!long.TryParse(
            unixTimeString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var unixTime))
        {
            throw new FormatException();
        }
        if (!sbyte.TryParse(
            offsetHourString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var hours))
        {
            throw new FormatException();
        }
        if (!byte.TryParse(
            offsetMinuteString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var minutes))
        {
            throw new FormatException();
        }
        var offset = new TimeSpan(hours, minutes, 0);
        var date = Utilities.FromUnixTimeSeconds(unixTime, offset);

        var mc = elements[dateIndex - 1];
        var mailAddress = mc.StartsWith("<") && mc.EndsWith(">") ?
            mc.Substring(1, mc.Length - 2) : null;

        var name = string.Join(" ",
            elements.Take(mailAddress != null ? dateIndex - 1 : dateIndex).ToArray());

        return new(name, mailAddress, date);
    }
}
