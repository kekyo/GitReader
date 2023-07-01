////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using GitReader.Internal;

namespace GitReader;

[DebuggerDisplay("{PrettyPrint}")]
public readonly struct Signature : IEquatable<Signature>
{
    private static readonly char[] separators = new[] { ' ' };

    public readonly string Name;
    public readonly string? MailAddress;
    public readonly DateTimeOffset Date;

    public Signature(
        string name, string? mailAddress, DateTimeOffset date)
    {
        this.Name = name;
        this.MailAddress = mailAddress;
        this.Date = Utilities.TruncateMilliseconds(date);
    }

    public Signature(
        string name, DateTimeOffset date)
    {
        this.Name = name;
        this.Date = Utilities.TruncateMilliseconds(date);
    }

    public Signature(
        string signatureString) =>
        this = Parse(signatureString);

    private string RawDate =>
        $"{this.Date.ToUnixTimeSeconds()} {this.Date.Offset:hhmm}";

    public string RawFormat =>
        $"{Utilities.ToGitAuthorString(this)} {this.RawDate}";

    private string PrettyPrint =>
        this.ToString();

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
        $"{Utilities.ToGitAuthorString(this)} {Utilities.ToGitDateString(this.Date)}";

    public static bool TryParse(string signatureString, out Signature sig)
    {
        var elements = signatureString.Split(
            separators, StringSplitOptions.RemoveEmptyEntries);

        if (elements.Length < 3)
        {
            sig = default;
            return false;
        }

        var dateIndex = elements.Length - 2;
        var unixTimeString = elements[dateIndex];
        var offsetString = elements[dateIndex + 1];

        if (offsetString.Length < 4)
        {
            sig = default;
            return false;
        }

        var offsetHourString = offsetString.Substring(0, offsetString.Length - 2);
        var offsetMinuteString = offsetString.Substring(offsetString.Length - 2, 2);

        if (!long.TryParse(
            unixTimeString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var unixTime))
        {
            sig = default;
            return false;
        }

        if (!sbyte.TryParse(
            offsetHourString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var hours))
        {
            sig = default;
            return false;
        }

        if (!byte.TryParse(
            offsetMinuteString,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var minutes))
        {
            sig = default;
            return false;
        }

        var offset = new TimeSpan(hours, minutes, 0);
        var date = Utilities.FromUnixTimeSeconds(unixTime, offset);

        var mc = elements[dateIndex - 1];
        var mailAddress = mc.StartsWith("<") && mc.EndsWith(">") ?
            mc.Substring(1, mc.Length - 2) : null;

        var name = string.Join(" ",
            elements.Take(mailAddress != null ? dateIndex - 1 : dateIndex).ToArray());

        sig = new(name, mailAddress, date);
        return true;
    }

    public static Signature Parse(string signatureString) =>
        TryParse(signatureString, out var sig) ?
            sig : throw new ArgumentException(nameof(signatureString));
}
