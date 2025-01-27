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

[<AutoOpen>]
module public DictionaryExtension =

    type Dictionary<'TKey, 'TValue> with
        member dict.asReadOnly() =
            ReadOnlyDictionary<_, _>(dict)

    type IReadOnlyDictionary<'TKey, 'TValue> with
        member dict.asReadOnly() =
            ReadOnlyDictionary<_, _>(
                dict.ToDictionary(
                    (fun entry -> entry.Key),
                    (fun entry -> entry.Value)))
