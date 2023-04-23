////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections

[<AutoOpen>]
module public ReadOnlyDictionaryExtension =

    type ReadOnlyDictionary<'TKey, 'TValue> with
        member dict.getValue(key: 'TKey) =
            match dict.TryGetValue(key) with
            | (true, value) -> Some value
            | _ -> None
