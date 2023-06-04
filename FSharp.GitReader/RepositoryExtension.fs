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
open System.Threading
open System.ComponentModel

[<AutoOpen>]
module public RepositoryExtension =

    type DateTimeOffset with
        member date.toGitDateString() =
            Utilities.ToGitDateString(date)
        member date.toGitIsoDateString() =
            Utilities.ToGitIsoDateString(date)

    type Signature with
        member signature.toGitAuthorString() =
            Utilities.ToGitAuthorString(signature)
    
    type Repository with
        [<EditorBrowsable(EditorBrowsableState.Advanced)>]
        member repository.openRawObjectStream(hash: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.OpenRawObjectStreamAsync(
                repository, hash, unwrapCT ct) |> Async.AwaitTask

    let (|Hash|) (hash: Hash) =
        (hash.HashCode, hash.ToString())

    let (|Signature|) (signature: Signature) =
        (signature.Name, signature.MailAddress, signature.Date)
