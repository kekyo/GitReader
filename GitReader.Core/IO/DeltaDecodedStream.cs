////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.IO;

internal sealed class DeltaDecodedStream : Stream
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    , IValueTaskStream
#endif
{
    private abstract class State
    {
    }

    private sealed class CopyState : State
    {
        public readonly uint Size;
        public uint Position;

        public CopyState(uint size) =>
            Size = size;
    }

    private sealed class InsertState : State
    {
        public readonly byte Size;
        public byte Position;

        public InsertState(byte size) =>
            Size = size;
    }

    private const int preloadBufferSize = 65536;

    private MemoizedStream baseObjectStream;
    private Stream deltaStream;
    private BufferPoolBuffer deltaBuffer;
    private int deltaBufferIndex;
    private int deltaBufferCount;

    private State? state;

    private DeltaDecodedStream(
        MemoizedStream baseObjectStream, Stream deltaStream,
        BufferPoolBuffer preloadBuffer, int preloadIndex, int preloadCount, long decodedObjectLength)
    {
        this.baseObjectStream = baseObjectStream;
        this.deltaStream = deltaStream;
        deltaBuffer = preloadBuffer;
        deltaBufferIndex = preloadIndex;
        deltaBufferCount = preloadCount;
        Length = decodedObjectLength;
    }

    public override long Length { get; }

    public override bool CanRead =>
        true;
    public override bool CanSeek =>
        false;
    public override bool CanWrite =>
        false;

#if !NETSTANDARD1_6
    public override void Close()
#else
    public void Close()
#endif
    {
        if (Interlocked.Exchange(ref this.baseObjectStream, null!) is { } baseObjectStream)
        {
            baseObjectStream.Dispose();
        }
        if (Interlocked.Exchange(ref this.deltaStream, null!) is { } deltaStream)
        {
            deltaStream.Dispose();
        }

        deltaBuffer.Dispose();
        state = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
    }

    private bool Prepare()
    {
        deltaBufferCount = deltaStream.Read(
            deltaBuffer, 0, deltaBuffer.Length);
        if (deltaBufferCount == 0)
        {
            return false;
        }

        deltaBufferIndex = 0;
        return true;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        while (count >= 1)
        {
            if (state == null)
            {
                if (deltaBufferIndex >= deltaBufferCount)
                {
                    if (!Prepare())
                    {
                        return read;
                    }
                }

                var opcode = deltaBuffer[deltaBufferIndex++];

                // Copy opcode
                if ((opcode & 0x80) != 0)
                {
                    var baseObjectOffset = 0U;
                    var baseObjectSize = 0U;

                    // offset1
                    if ((opcode & 0x01) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= deltaBuffer[deltaBufferIndex++];
                    }
                    // offset2
                    if ((opcode & 0x02) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 8;
                    }
                    // offset3
                    if ((opcode & 0x04) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 16;
                    }
                    // offset4
                    if ((opcode & 0x08) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 24;
                    }
                    // size1
                    if ((opcode & 0x10) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= deltaBuffer[deltaBufferIndex++];
                    }
                    // size2
                    if ((opcode & 0x20) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= (uint)deltaBuffer[deltaBufferIndex++] << 8;
                    }
                    // size3
                    if ((opcode & 0x40) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!Prepare())
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= (uint)deltaBuffer[deltaBufferIndex++] << 16;
                    }

                    baseObjectStream.Seek(
                        baseObjectOffset, SeekOrigin.Begin);

                    state = new CopyState(baseObjectSize);
                }
                // Insert opcode
                else
                {
                    // Size
                    var insertionSize = opcode & 0x7f;

                    state = new InsertState((byte)insertionSize);
                }
            }

            if (state is CopyState copyState)
            {
                var baseObjectRemains =
                    copyState.Size - copyState.Position;
                if (baseObjectRemains <= 0)
                {
                    state = null;
                    continue;
                }

                var length = Math.Min(baseObjectRemains, count);

                var r = baseObjectStream.Read(
                    buffer, offset, (int)length);

                if (r == 0)
                {
                    return read;
                }

                offset += r;
                count -= r;
                read += r;

                copyState.Position += (uint)r;
            }
            else
            {
                var insertState = (InsertState)state;

                var insertionRemains =
                    insertState.Size - insertState.Position;
                if (insertionRemains <= 0)
                {
                    state = null;
                    continue;
                }

                var length = Math.Min(insertionRemains, count);

                while (length >= 1)
                {
                    Debug.Assert(length < byte.MaxValue);

                    if (deltaBufferIndex >= deltaBufferCount)
                    {
                        if (!Prepare())
                        {
                            return read;
                        }
                    }

                    var l = Math.Min(
                        length, deltaBufferCount - deltaBufferIndex);

                    Array.Copy(
                        deltaBuffer, deltaBufferIndex,
                        buffer, offset, l);

                    offset += l;
                    count -= l;
                    read += l;
                    deltaBufferIndex += l;

                    insertState.Position += (byte)l;
                    length -= l;
                }
            }
        }

        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<bool> PrepareAsync(CancellationToken ct)
    {
        deltaBufferCount = deltaStream is IValueTaskStream vts ?
            await vts.ReadValueTaskAsync(
                deltaBuffer, 0, deltaBuffer.Length, ct) :
            await deltaStream.ReadAsync(
                deltaBuffer, 0, deltaBuffer.Length, ct);

        if (deltaBufferCount == 0)
        {
            return false;
        }

        deltaBufferIndex = 0;
        return true;
    }

    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;

        while (count >= 1)
        {
            if (state == null)
            {
                if (deltaBufferIndex >= deltaBufferCount)
                {
                    if (!await PrepareAsync(ct))
                    {
                        return read;
                    }
                }

                var opcode = deltaBuffer[deltaBufferIndex++];

                // Copy opcode
                if ((opcode & 0x80) != 0)
                {
                    var baseObjectOffset = 0U;
                    var baseObjectSize = 0U;

                    // offset1
                    if ((opcode & 0x01) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= deltaBuffer[deltaBufferIndex++];
                    }
                    // offset2
                    if ((opcode & 0x02) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 8;
                    }
                    // offset3
                    if ((opcode & 0x04) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 16;
                    }
                    // offset4
                    if ((opcode & 0x08) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 24;
                    }
                    // size1
                    if ((opcode & 0x10) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= deltaBuffer[deltaBufferIndex++];
                    }
                    // size2
                    if ((opcode & 0x20) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= (uint)deltaBuffer[deltaBufferIndex++] << 8;
                    }
                    // size3
                    if ((opcode & 0x40) != 0)
                    {
                        if (deltaBufferIndex >= deltaBufferCount)
                        {
                            if (!await PrepareAsync(ct))
                            {
                                return read;
                            }
                        }
                        baseObjectSize |= (uint)deltaBuffer[deltaBufferIndex++] << 16;
                    }

                    await baseObjectStream.SeekValueTaskAsync(
                        baseObjectOffset, SeekOrigin.Begin, ct);

                    state = new CopyState(baseObjectSize);
                }
                // Insert opcode
                else
                {
                    // Size
                    var insertionSize = opcode & 0x7f;

                    state = new InsertState((byte)insertionSize);
                }
            }

            if (state is CopyState copyState)
            {
                var baseObjectRemains =
                    copyState.Size - copyState.Position;
                if (baseObjectRemains <= 0)
                {
                    state = null;
                    continue;
                }

                var length = Math.Min(baseObjectRemains, count);

                Debug.Assert(length <= uint.MaxValue);

                var r = await baseObjectStream.ReadValueTaskAsync(
                    buffer, offset, (int)length, ct);

                if (r == 0)
                {
                    return read;
                }

                offset += r;
                count -= r;
                read += r;

                copyState.Position += (uint)r;
            }
            else
            {
                var insertState = (InsertState)state;

                var insertionRemains =
                    insertState.Size - insertState.Position;
                if (insertionRemains <= 0)
                {
                    state = null;
                    continue;
                }

                var length = Math.Min(insertionRemains, count);

                while (length >= 1)
                {
                    Debug.Assert(length <= byte.MaxValue);

                    if (deltaBufferIndex >= deltaBufferCount)
                    {
                        if (!await PrepareAsync(ct))
                        {
                            return read;
                        }
                    }

                    var l = Math.Min(
                        length, deltaBufferCount - deltaBufferIndex);

                    Array.Copy(
                        deltaBuffer, deltaBufferIndex,
                        buffer, offset, l);

                    offset += l;
                    count -= l;
                    read += l;
                    deltaBufferIndex += l;

                    insertState.Position += (byte)l;
                    length -= l;
                }
            }
        }

        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
#endif

    public override long Position
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotImplementedException();

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public ValueTask<long> SeekValueTaskAsync(
        long offset, SeekOrigin origin, CancellationToken ct) =>
        throw new NotImplementedException();
#endif

    public override void SetLength(long value) =>
        throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotImplementedException();

    public override void Flush() =>
        throw new NotImplementedException();

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<DeltaDecodedStream> CreateAsync(
        Stream baseObjectStream, Stream deltaStream, CancellationToken ct)
#else
    public static async Task<DeltaDecodedStream> CreateAsync(
        Stream baseObjectStream, Stream deltaStream, CancellationToken ct)
#endif
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. Step={step}");

        var preloadIndex = 0;
        var preloadBuffer = BufferPool.Take(preloadBufferSize);

        var read = await deltaStream.ReadAsync(preloadBuffer, 0, preloadBuffer.Length, ct);
        if (read == 0)
        {
            Throw(1);
        }

        if (!ObjectAccessor.TryGetVariableSize(
            preloadBuffer, read, ref preloadIndex, out var baseObjectLength, 7))
        {
            Throw(2);
        }
        if (baseObjectLength > long.MaxValue)
        {
            Throw(3);
        }

        if (!ObjectAccessor.TryGetVariableSize(
            preloadBuffer, read, ref preloadIndex, out var decodedObjectLength, 7))
        {
            Throw(3);
        }
        if (decodedObjectLength > long.MaxValue)
        {
            Throw(3);
        }

        return new(
            await MemoizedStream.CreateAsync(baseObjectStream, (long)baseObjectLength, ct),
            deltaStream,
            preloadBuffer, preloadIndex, read, (long)decodedObjectLength);
    }
}
