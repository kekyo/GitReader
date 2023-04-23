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
module public DictionaryExtension =

    type Dictionary<'TKey, 'TValue> with
        member dict.asReadOnly() =
            new ReadOnlyDictionary<_, _>(dict)
