////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

namespace GitReader;

public enum FileStatus
{
    Unmodified = 0,
    Added = 1,
    Modified = 2,
    Deleted = 3,
    Renamed = 4,
    Copied = 5,
    Untracked = 6,
    Ignored = 7,
}
