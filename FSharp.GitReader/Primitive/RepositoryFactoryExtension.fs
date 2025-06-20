////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader.Primitive

open System.Runtime.CompilerServices
open GitReader
open GitReader.IO
open System.Threading

/// <summary>
/// Provides F#-specific extension methods for creating primitive repository instances.
/// </summary>
[<AutoOpen>]
module public RepositoryFactoryExtension =

    type RepositoryFactory with
        /// <summary>
        /// Opens a primitive repository at the specified path using the default file system.
        /// </summary>
        /// <param name="path">The path to the repository.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns a PrimitiveRepository instance.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member _.openPrimitive(path: string, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.OpenPrimitiveAsync(
                path, new StandardFileSystem(65536), unwrapCT ct).asAsync()
        /// <summary>
        /// Opens a primitive repository at the specified path using a custom file system.
        /// </summary>
        /// <param name="path">The path to the repository.</param>
        /// <param name="fileSystem">The file system implementation to use.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>An async computation that returns a PrimitiveRepository instance.</returns>
        [<MethodImpl(MethodImplOptions.NoInlining)>]
        member _.openPrimitive(path: string, fileSystem: IFileSystem, ?ct: CancellationToken) =
            PrimitiveRepositoryFacade.OpenPrimitiveAsync(
                path, fileSystem, unwrapCT ct).asAsync()
