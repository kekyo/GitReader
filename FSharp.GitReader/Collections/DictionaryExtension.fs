////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections

open System.Collections.Generic
open System.Linq

/// <summary>
/// Provides F#-specific extension methods for dictionary and ReadOnlyDictionary operations.
/// </summary>
[<AutoOpen>]
module public DictionaryExtension =

    type Dictionary<'TKey, 'TValue> with
        /// <summary>
        /// Converts a Dictionary to a ReadOnlyDictionary.
        /// </summary>
        /// <returns>A ReadOnlyDictionary wrapping the original dictionary.</returns>
        member dict.asReadOnly() =
            ReadOnlyDictionary<_, _>(dict)

    type IReadOnlyDictionary<'TKey, 'TValue> with
        /// <summary>
        /// Converts an IReadOnlyDictionary to a ReadOnlyDictionary by creating a new Dictionary.
        /// </summary>
        /// <returns>A ReadOnlyDictionary wrapping a new dictionary with copied key-value pairs.</returns>
        member dict.asReadOnly() =
            ReadOnlyDictionary<_, _>(
                dict.ToDictionary(
                    (fun entry -> entry.Key),
                    (fun entry -> entry.Value)))
