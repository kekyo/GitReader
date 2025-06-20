////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader

open GitReader.Internal
open System
open System.Threading
open System.ComponentModel

/// <summary>
/// Provides F#-specific extension methods for repository-related functionality.
/// </summary>
[<AutoOpen>]
module public RepositoryExtension =

    type DateTimeOffset with
        /// <summary>
        /// Converts a DateTimeOffset to Git's standard date string format.
        /// </summary>
        /// <returns>A string representation of the date in Git format.</returns>
        member date.toGitDateString() =
            Utilities.ToGitDateString(date)
        
        /// <summary>
        /// Converts a DateTimeOffset to Git's ISO date string format.
        /// </summary>
        /// <returns>A string representation of the date in ISO format.</returns>
        member date.toGitIsoDateString() =
            Utilities.ToGitIsoDateString(date)

    type Signature with
        /// <summary>
        /// Converts a Signature to Git's author string format.
        /// </summary>
        /// <returns>A string representation of the signature in Git author format.</returns>
        member signature.toGitAuthorString() =
            Utilities.ToGitAuthorString(signature)
    
    type Repository with
        /// <summary>
        /// Opens a raw object stream for the specified object ID.
        /// </summary>
        /// <param name="objectId">The hash of the object to open.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns an ObjectStreamResult containing the stream and object type.</returns>
        [<EditorBrowsable(EditorBrowsableState.Advanced)>]
        member repository.openRawObjectStream(objectId: Hash, ?ct: CancellationToken) =
            RepositoryAccessor.OpenRawObjectStreamAsync(
                repository, objectId, unwrapCT ct).asAsync()

    /// <summary>
    /// Active pattern for deconstructing a Hash into its byte array and string representations.
    /// </summary>
    /// <param name="hash">The hash to deconstruct.</param>
    /// <returns>A tuple containing the hash code as byte array and string representation.</returns>
    let (|Hash|) (hash: Hash) =
        (hash.HashCode, hash.ToString())

    /// <summary>
    /// Active pattern for deconstructing a Signature into its component parts.
    /// </summary>
    /// <param name="signature">The signature to deconstruct.</param>
    /// <returns>A tuple containing the name, optional email address, and date.</returns>
    let (|Signature|) (signature: Signature) =
        (signature.Name, signature.MailAddress, signature.Date)
