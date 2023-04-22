////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Primitive;

public static class RepositoryFactoryExtension
{
    public static Task<Repository> OpenAsync(
        this RepositoryFactory _,
        string path, CancellationToken ct = default, bool forceUnlock = false) =>
        RepositoryFacade.OpenPrimitiveAsync(path, ct, forceUnlock);
}
