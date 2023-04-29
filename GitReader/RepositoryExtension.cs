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

    public static void Deconstruct(
        this Signature signature,
        out string name, out string? mailAddress, out DateTimeOffset date)
    {
        name = signature.Name;
        mailAddress = signature.MailAddress;
        date = signature.Date;
    }
}
