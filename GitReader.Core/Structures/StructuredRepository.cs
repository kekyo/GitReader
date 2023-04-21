////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;

namespace GitReader.Structures;

public sealed class StructuredRepository : Repository
{
    internal StructuredRepository(
        string repositoryPath, TemporaryFile locker) :
        base(repositoryPath, locker)
    {
    }
}
