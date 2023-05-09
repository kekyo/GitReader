////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections

open System.Collections.Generic

[<AutoOpen>]
module public ReadOnlyDictionaryExtension =

    type ReadOnlyDictionary<'TKey, 'TValue> with
        member dict.containsKey(key: 'TKey) =
            dict.parent.ContainsKey(key)

        member dict.containsValue(value: 'TValue) =
            dict.parent.ContainsValue(value)

        member dict.getValue(key: 'TKey) =
            match dict.parent.TryGetValue(key) with
            | (true, value) -> Some value
            | _ -> None

        member dict.clone() =
            dict.Clone()
