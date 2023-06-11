////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
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

public static class RepositoryExtension
{
    public static string ToGitDateString(
        this DateTimeOffset date) =>
        Utilities.ToGitDateString(date);

    public static string ToGitIsoDateString(
        this DateTimeOffset date) =>
        Utilities.ToGitIsoDateString(date);

    public static string ToGitAuthorString(
        this Signature signature) =>
        Utilities.ToGitAuthorString(signature);

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Task<ObjectStreamResult> OpenRawObjectStreamAsync(
        this Repository repository,
        Hash objectId,
        CancellationToken ct = default) =>
        RepositoryAccessor.OpenRawObjectStreamAsync(repository, objectId, ct);

    public static void Deconstruct(
        this Hash hash, out byte[] hashCode) =>
        hashCode = hash.HashCode;

    public static void Deconstruct(
        this Hash hash, out string hashString) =>
        hashString = hash.ToString();

    public static void Deconstruct(
        this Signature signature,
        out string name, out string? mailAddress, out DateTimeOffset date)
    {
        name = signature.Name;
        mailAddress = signature.MailAddress;
        date = signature.Date;
    }

    public static void Deconstruct(
        this ObjectStreamResult result,
        out Stream stream,
        out ObjectTypes type)
    {
        stream = result.Stream;
        type = result.Type;
    }
}
