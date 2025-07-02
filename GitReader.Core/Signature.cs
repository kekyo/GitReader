////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
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

/// <summary>
/// Represents a Git signature containing author or committer information.
/// </summary>
[DebuggerDisplay("{PrettyPrint}")]
public readonly struct Signature : IEquatable<Signature>
{
    private static readonly char[] separators = new[] { ' ' };

    /// <summary>
    /// The name of the person associated with this signature.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The email address of the person associated with this signature.
    /// </summary>
    public readonly string? MailAddress;
    
    /// <summary>
    /// The date and time when this signature was created.
    /// </summary>
    public readonly DateTimeOffset Date;

    /// <summary>
    /// Initializes a new instance of the Signature struct with name, email address, and date.
    /// </summary>
    /// <param name="name">The name of the person.</param>
    /// <param name="mailAddress">The email address of the person.</param>
    /// <param name="date">The date and time of the signature.</param>
    public Signature(
        string name, string? mailAddress, DateTimeOffset date)
    {
        this.Name = name;
        this.MailAddress = mailAddress;
        this.Date = Utilities.TruncateMilliseconds(date);
    }

    /// <summary>
    /// Initializes a new instance of the Signature struct with name and date (without email address).
    /// </summary>
    /// <param name="name">The name of the person.</param>
    /// <param name="date">The date and time of the signature.</param>
    public Signature(
        string name, DateTimeOffset date)
    {
        this.Name = name;
        this.Date = Utilities.TruncateMilliseconds(date);
    }

    /// <summary>
    /// Initializes a new instance of the Signature struct by parsing a signature string.
    /// </summary>
    /// <param name="signatureString">The signature string to parse.</param>
    /// <exception cref="ArgumentException">Thrown when the signature string is invalid.</exception>
    public Signature(
        string signatureString) =>
        this = Parse(signatureString);

    private string RawDate =>
        Utilities.ToGitRawDateString(this.Date);

    /// <summary>
    /// Gets the raw format representation of the signature as used in Git objects.
    /// </summary>
    public string RawFormat =>
        $"{Utilities.ToGitAuthorString(this)} {this.RawDate}";

    private string PrettyPrint =>
        this.ToString();

    /// <summary>
    /// Determines whether the specified Signature is equal to the current Signature.
    /// </summary>
    /// <param name="rhs">The Signature to compare with the current Signature.</param>
    /// <returns>true if the specified Signature is equal to the current Signature; otherwise, false.</returns>
    public bool Equals(Signature rhs) =>
        this.Name == rhs.Name &&
        this.MailAddress == rhs.MailAddress &&
        this.Date == rhs.Date;

    bool IEquatable<Signature>.Equals(Signature rhs) =>
        Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current Signature.
    /// </summary>
    /// <param name="obj">The object to compare with the current Signature.</param>
    /// <returns>true if the specified object is equal to the current Signature; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Signature rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() =>
        this.Name.GetHashCode() ^
        (this.MailAddress?.GetHashCode() ?? 0) ^
        this.Date.GetHashCode();

    /// <summary>
    /// Returns a string representation of the signature.
    /// </summary>
    /// <returns>A string representation of the signature.</returns>
    public override string ToString() =>
        $"{Utilities.ToGitAuthorString(this)} {Utilities.ToGitDateString(this.Date)}";

    /// <summary>
    /// Tries to parse a signature string into a Signature instance.
    /// </summary>
    /// <param name="signatureString">The signature string to parse.</param>
    /// <param name="sig">When this method returns, contains the parsed Signature if the conversion succeeded, or a default Signature if the conversion failed.</param>
    /// <returns>true if the conversion succeeded; otherwise, false.</returns>
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

    /// <summary>
    /// Parses a signature string into a Signature instance.
    /// </summary>
    /// <param name="signatureString">The signature string to parse.</param>
    /// <returns>A Signature instance parsed from the string.</returns>
    /// <exception cref="ArgumentException">Thrown when the signature string is invalid.</exception>
    public static Signature Parse(string signatureString) =>
        TryParse(signatureString, out var sig) ?
            sig : throw new ArgumentException(nameof(signatureString));
}
