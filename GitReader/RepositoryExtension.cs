////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace GitReader;

/// <summary>
/// Provides extension methods for repository-related functionality.
/// </summary>
public static class RepositoryExtension
{
    /// <summary>
    /// Converts a DateTimeOffset to Git's standard date string format.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>A string representation of the date in Git format.</returns>
    public static string ToGitDateString(
        this DateTimeOffset date) =>
        Utilities.ToGitDateString(date);

    /// <summary>
    /// Converts a DateTimeOffset to Git's ISO date string format.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>A string representation of the date in ISO format.</returns>
    public static string ToGitIsoDateString(
        this DateTimeOffset date) =>
        Utilities.ToGitIsoDateString(date);

    /// <summary>
    /// Converts a DateTimeOffset to Git's raw date string format.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>A string representation of the date in raw format.</returns>
    public static string ToGitRawDateString(
        this DateTimeOffset date) =>
        Utilities.ToGitRawDateString(date);

    /// <summary>
    /// Converts a Signature to Git's author string format.
    /// </summary>
    /// <param name="signature">The signature to convert.</param>
    /// <returns>A string representation of the signature in Git author format.</returns>
    public static string ToGitAuthorString(
        this Signature signature) =>
        Utilities.ToGitAuthorString(signature);

    /// <summary>
    /// Opens a raw object stream for the specified object ID.
    /// </summary>
    /// <param name="repository">The repository to access.</param>
    /// <param name="objectId">The hash of the object to open.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that returns an ObjectStreamResult containing the stream and object type.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Task<ObjectStreamResult> OpenRawObjectStreamAsync(
        this Repository repository,
        Hash objectId,
        CancellationToken ct = default) =>
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        RepositoryAccessor.OpenRawObjectStreamAsync(repository, objectId, ct).AsTask();
#else
        RepositoryAccessor.OpenRawObjectStreamAsync(repository, objectId, ct);
#endif

    /// <summary>
    /// Deconstructs a Hash into its byte array representation.
    /// </summary>
    /// <param name="hash">The hash to deconstruct.</param>
    /// <param name="hashCode">The byte array representation of the hash.</param>
    public static void Deconstruct(
        this Hash hash, out byte[] hashCode) =>
        hashCode = hash.HashCode;

    /// <summary>
    /// Deconstructs a Hash into its string representation.
    /// </summary>
    /// <param name="hash">The hash to deconstruct.</param>
    /// <param name="hashString">The string representation of the hash.</param>
    public static void Deconstruct(
        this Hash hash, out string hashString) =>
        hashString = hash.ToString();

    /// <summary>
    /// Deconstructs a Signature into its component parts.
    /// </summary>
    /// <param name="signature">The signature to deconstruct.</param>
    /// <param name="name">The name component of the signature.</param>
    /// <param name="mailAddress">The email address component of the signature.</param>
    /// <param name="date">The date component of the signature.</param>
    public static void Deconstruct(
        this Signature signature,
        out string name, out string? mailAddress, out DateTimeOffset date)
    {
        name = signature.Name;
        mailAddress = signature.MailAddress;
        date = signature.Date;
    }

    /// <summary>
    /// Deconstructs an ObjectStreamResult into its component parts.
    /// </summary>
    /// <param name="result">The ObjectStreamResult to deconstruct.</param>
    /// <param name="stream">The stream component of the result.</param>
    /// <param name="type">The object type component of the result.</param>
    public static void Deconstruct(
        this ObjectStreamResult result,
        out Stream stream,
        out ObjectTypes type)
    {
        stream = result.Stream;
        type = result.Type;
    }
}
