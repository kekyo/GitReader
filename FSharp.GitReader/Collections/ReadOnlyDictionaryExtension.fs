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

/// <summary>
/// Provides F#-specific extension methods for ReadOnlyDictionary operations.
/// </summary>
[<AutoOpen>]
module public ReadOnlyDictionaryExtension =

    type ReadOnlyDictionary<'TKey, 'TValue> with
        /// <summary>
        /// Determines whether the ReadOnlyDictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>true if the dictionary contains the key; otherwise, false.</returns>
        member dict.containsKey(key: 'TKey) =
            dict.parent.ContainsKey(key)

        /// <summary>
        /// Determines whether the ReadOnlyDictionary contains the specified value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>true if the dictionary contains the value; otherwise, false.</returns>
        member dict.containsValue(value: 'TValue) =
            dict.parent.ContainsValue(value)

        /// <summary>
        /// Gets the value associated with the specified key as an Option.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>Some value if the key is found; otherwise, None.</returns>
        member dict.getValue(key: 'TKey) =
            match dict.parent.TryGetValue(key) with
            | (true, value) -> Some value
            | _ -> None

        /// <summary>
        /// Creates a shallow copy of the ReadOnlyDictionary as a regular Dictionary.
        /// </summary>
        /// <returns>A new Dictionary containing copies of all key-value pairs.</returns>
        member dict.clone() =
            dict.Clone()
