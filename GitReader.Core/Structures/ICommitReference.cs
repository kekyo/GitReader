////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.ComponentModel;

namespace GitReader.Structures;

public interface ICommitReference
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Hash Hash { get; }
}

internal interface IInternalCommitReference :
    ICommitReference, IRepositoryReference
{
}
