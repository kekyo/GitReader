////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Structures

open GitReader
open GitReader.IO
open System.Threading

/// <summary>
/// Provides F#-specific extension methods for creating structured repository instances.
/// </summary>
[<AutoOpen>]
module public RepositoryFactoryExtension =

    type RepositoryFactory with
        /// <summary>
        /// Opens a structured repository at the specified path using the default file system.
        /// </summary>
        /// <param name="path">The path to the repository.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns a StructuredRepository instance.</returns>
        member _.openStructured(path: string, ?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenStructuredAsync(
                path, new StandardFileSystem(65536), unwrapCT ct).asAsync()
        
        /// <summary>
        /// Opens a structured repository at the specified path using a custom file system.
        /// </summary>
        /// <param name="path">The path to the repository.</param>
        /// <param name="fileSystem">The file system implementation to use.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns a StructuredRepository instance.</returns>
        member _.openStructured(path: string, fileSystem: IFileSystem, ?ct: CancellationToken) =
            StructuredRepositoryFacade.OpenStructuredAsync(
                path, fileSystem, unwrapCT ct).asAsync()
