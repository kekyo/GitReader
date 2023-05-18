////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
// Minimize awaitable cost for chatty reading.
internal interface IValueTaskStream
{
    ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct);
    ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct);
}
#endif
