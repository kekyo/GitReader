﻿////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.IO;

namespace GitReader.Primitive;

public sealed class PrimitiveRepository : Repository
{
    internal PrimitiveRepository(
        string repositoryPath,
        IFileSystem fileSystem) :
        base(repositoryPath, fileSystem)
    {
    }
}
