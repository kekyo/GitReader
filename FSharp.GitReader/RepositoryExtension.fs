////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader

open GitReader.Internal
open System

[<AutoOpen>]
module public RepositoryExtension =

    type DateTimeOffset with
        member date.toGitDateString() =
            Utilities.ToGitDateString(date)

    type Signature with
        member signature.toGitAuthorString() =
            Utilities.ToGitAuthorString(signature)

    let (|Signature|) (signature: Signature) =
        (signature.Name, signature.MailAddress, signature.Date)
