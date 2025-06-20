////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Collections

/// <summary>
/// Provides F#-specific extension methods for ReadOnlyArray operations.
/// </summary>
[<AutoOpen>]
module public ReadOnlyArrayExtension =

    type ReadOnlyArray<'TValue> with
        /// <summary>
        /// Determines whether the ReadOnlyArray contains a specific value.
        /// </summary>
        /// <param name="item">The value to locate.</param>
        /// <returns>true if the array contains the item; otherwise, false.</returns>
        member array.contains(item: 'TValue) =
            array.Contains(item)

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="item">The value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of item, or -1 if not found.</returns>
        member array.indexOf(item: 'TValue) =
            array.IndexOf(item)

        /// <summary>
        /// Creates a shallow copy of the ReadOnlyArray as a regular array.
        /// </summary>
        /// <returns>A new array containing copies of all elements.</returns>
        member array.clone() =
            array.Clone()
