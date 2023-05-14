////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.IO;

internal static class ZLibStream
{
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<Stream> CreateAsync(
        Stream parent, CancellationToken ct)
#else
    public static async Task<Stream> CreateAsync(
        Stream parent, CancellationToken ct)
#endif
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse zlib stream. Step={step}");

        using var buffer = BufferPool.Take(2);

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        var read = parent is IValueTaskStream vts ?
            await vts.ReadValueTaskAsync(buffer, 0, buffer.Length, ct) :
            await parent.ReadAsync(buffer, 0, buffer.Length, ct);
#else
        var read = await parent.ReadAsync(buffer, 0, buffer.Length, ct);
#endif

        if (read < 2)
        {
            Throw(1);
        }

        if (buffer[0] != 0x78)
        {
            Throw(2);
        }

        switch (buffer[1])
        {
            case 0x01:
            case 0x5e:
            case 0x9c:
            case 0xda:
                break;
            default:
                Throw(3);
                break;
        }

        // TODO: One way to improve performance is to make DelateStream compatible
        // with IValueTaskStream from in to out.
        // This is a low priority since it would mean re-implementing DeflateStream,
        // but from observation, it may make sense to implement it
        // since ReadAsync calls from within are chatty.
        return new DeflateStream(
            parent, CompressionMode.Decompress, false);
    }
}
