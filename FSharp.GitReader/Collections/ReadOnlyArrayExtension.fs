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
module public ReadOnlyArrayExtension =

    type ReadOnlyArray<'TValue> with
        member array.contains(item: 'TValue) =
            array.Contains(item)

        member array.indexOf(item: 'TValue) =
            array.IndexOf(item)

        member array.clone() =
            array.Clone()
