////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
#if DEBUG
        public readonly uint Offset;
#endif

        public CopyState(uint offset, uint size)
        {
            this.Size = size;
#if DEBUG
            this.Offset = offset;
#endif
        }

#if DEBUG
        public override string ToString() =>
            $"Copy: Offset=0x{this.Offset:x}, Size={this.Size}";
#endif
    }

    private sealed class InsertState : State
    {
        public readonly byte Size;
        public byte Position;

        public InsertState(byte size) =>
            this.Size = size;

#if DEBUG
        public override string ToString() =>
            $"Insert: Size={Size}";
#endif
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
        DetachedBufferPoolBuffer preloadBuffer, int preloadIndex, int preloadCount, long decodedObjectLength)
    {
        this.baseObjectStream = baseObjectStream;
        this.deltaStream = deltaStream;
        this.deltaBuffer = preloadBuffer;
        this.deltaBufferIndex = preloadIndex;
        this.deltaBufferCount = preloadCount;
        this.Length = decodedObjectLength;
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

        this.deltaBuffer.Dispose();
        this.state = null;
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
        this.deltaBufferCount = this.deltaStream.Read(
            this.deltaBuffer, 0, this.deltaBuffer.Length);
        if (this.deltaBufferCount == 0)
        {
            return false;
        }

        this.deltaBufferIndex = 0;
        return true;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = 0;

        while (count >= 1)
        {
            if (this.state == null)
            {
                if (this.deltaBufferIndex >= this.deltaBufferCount)
                {
                    if (!this.Prepare())
                    {
                        return read;
                    }
                }

                var opcode = this.deltaBuffer[this.deltaBufferIndex++];

                // Copy opcode
                if ((opcode & 0x80) != 0)
                {
                    var baseObjectOffset = 0U;
                    var baseObjectSize = 0U;

                    // offset1
                    if ((opcode & 0x01) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=1");
                            }
                        }
                        baseObjectOffset |= this.deltaBuffer[this.deltaBufferIndex++];
                    }
                    // offset2
                    if ((opcode & 0x02) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=2");
                            }
                        }
                        baseObjectOffset |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 8;
                    }
                    // offset3
                    if ((opcode & 0x04) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=3");
                            }
                        }
                        baseObjectOffset |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 16;
                    }
                    // offset4
                    if ((opcode & 0x08) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=4");
                            }
                        }
                        baseObjectOffset |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 24;
                    }
                    // size1
                    if ((opcode & 0x10) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=5");
                            }
                        }
                        baseObjectSize |= this.deltaBuffer[this.deltaBufferIndex++];
                    }
                    // size2
                    if ((opcode & 0x20) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=6");
                            }
                        }
                        baseObjectSize |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 8;
                    }
                    // size3
                    if ((opcode & 0x40) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!this.Prepare())
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=7");
                            }
                        }
                        baseObjectSize |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 16;
                    }
                    // Adjust size when its zero
                    if (baseObjectSize == 0)
                    {
                        baseObjectSize = 0x10000U;
                    }

                    this.baseObjectStream.Seek(
                        baseObjectOffset, SeekOrigin.Begin);

                    this.state = new CopyState(baseObjectOffset, baseObjectSize);
                }
                // Insert opcode
                else
                {
                    // Size
                    var insertionSize = opcode & 0x7f;

                    this.state = new InsertState((byte)insertionSize);
                }
            }

            if (this.state is CopyState copyState)
            {
                var baseObjectRemains =
                    copyState.Size - copyState.Position;
                Debug.Assert(baseObjectRemains >= 1);

                var length = Math.Min(baseObjectRemains, count);

                var r = this.baseObjectStream.Read(
                    buffer, offset, (int)length);

                if (r == 0)
                {
                    return read;
                }

                offset += r;
                count -= r;
                read += r;

                copyState.Position += (uint)r;
                baseObjectRemains -= (uint)r;

                if (baseObjectRemains <= 0)
                {
                    this.state = null;
                }
            }
            else
            {
                var insertState = (InsertState)this.state;

                var insertionRemains =
                    insertState.Size - insertState.Position;
                Debug.Assert(insertionRemains >= 1);

                var length = Math.Min(insertionRemains, count);

                while (length >= 1)
                {
                    Debug.Assert(length < byte.MaxValue);

                    if (this.deltaBufferIndex >= this.deltaBufferCount)
                    {
                        if (!this.Prepare())
                        {
                            throw new InvalidDataException(
                                "Broken deltified stream: Step=8");
                        }
                    }

                    var l = Math.Min(
                        length, this.deltaBufferCount - this.deltaBufferIndex);

                    Array.Copy(
                        this.deltaBuffer, this.deltaBufferIndex,
                        buffer, offset, l);

                    offset += l;
                    count -= l;
                    read += l;
                    this.deltaBufferIndex += l;

                    insertState.Position += (byte)l;
                    length -= l;
                    insertionRemains -= l;
                }

                if (insertionRemains <= 0)
                {
                    this.state = null;
                }
            }
        }

        return read;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    private async ValueTask<bool> PrepareAsync(CancellationToken ct)
    {
        this.deltaBufferCount = this.deltaStream is IValueTaskStream vts ?
            await vts.ReadValueTaskAsync(
                this.deltaBuffer, 0, this.deltaBuffer.Length, ct) :
            await this.deltaStream.ReadAsync(
                this.deltaBuffer, 0, this.deltaBuffer.Length, ct);

        if (this.deltaBufferCount == 0)
        {
            return false;
        }

        this.deltaBufferIndex = 0;
        return true;
    }

    public async ValueTask<int> ReadValueTaskAsync(
        byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = 0;

        while (count >= 1)
        {
            if (this.state == null)
            {
                if (this.deltaBufferIndex >= this.deltaBufferCount)
                {
                    if (!await this.PrepareAsync(ct))
                    {
                        return read;
                    }
                }

                var opcode = this.deltaBuffer[this.deltaBufferIndex++];

                // Copy opcode
                if ((opcode & 0x80) != 0)
                {
                    var baseObjectOffset = 0U;
                    var baseObjectSize = 0U;

                    // offset1
                    if ((opcode & 0x01) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=1");
                            }
                        }
                        baseObjectOffset |= this.deltaBuffer[this.deltaBufferIndex++];
                    }
                    // offset2
                    if ((opcode & 0x02) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=2");
                            }
                        }
                        baseObjectOffset |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 8;
                    }
                    // offset3
                    if ((opcode & 0x04) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=3");
                            }
                        }
                        baseObjectOffset |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 16;
                    }
                    // offset4
                    if ((opcode & 0x08) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=4");
                            }
                        }
                        baseObjectOffset |= (uint)deltaBuffer[deltaBufferIndex++] << 24;
                    }
                    // size1
                    if ((opcode & 0x10) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=5");
                            }
                        }
                        baseObjectSize |= this.deltaBuffer[this.deltaBufferIndex++];
                    }
                    // size2
                    if ((opcode & 0x20) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=6");
                            }
                        }
                        baseObjectSize |= (uint)deltaBuffer[this.deltaBufferIndex++] << 8;
                    }
                    // size3
                    if ((opcode & 0x40) != 0)
                    {
                        if (this.deltaBufferIndex >= this.deltaBufferCount)
                        {
                            if (!await this.PrepareAsync(ct))
                            {
                                throw new InvalidDataException(
                                    "Broken deltified stream: Step=7");
                            }
                        }
                        baseObjectSize |= (uint)this.deltaBuffer[this.deltaBufferIndex++] << 16;
                    }
                    // Adjust size when its zero
                    if (baseObjectSize == 0)
                    {
                        baseObjectSize = 0x10000U;
                    }

                    await this.baseObjectStream.SeekValueTaskAsync(
                        baseObjectOffset, SeekOrigin.Begin, ct);

                    this.state = new CopyState(baseObjectOffset, baseObjectSize);
                }
                // Insert opcode
                else
                {
                    // Size
                    var insertionSize = opcode & 0x7f;

                    this.state = new InsertState((byte)insertionSize);
                }
            }

            if (this.state is CopyState copyState)
            {
                var baseObjectRemains =
                    copyState.Size - copyState.Position;
                Debug.Assert(baseObjectRemains >= 1);

                var length = Math.Min(baseObjectRemains, count);

                Debug.Assert(length <= uint.MaxValue);

                var r = await this.baseObjectStream.ReadValueTaskAsync(
                    buffer, offset, (int)length, ct);
                if (r == 0)
                {
                    return read;
                }

                offset += r;
                count -= r;
                read += r;

                copyState.Position += (uint)r;
                baseObjectRemains -= (uint)r;

                if (baseObjectRemains <= 0)
                {
                    this.state = null;
                }
            }
            else
            {
                var insertState = (InsertState)this.state;

                var insertionRemains =
                    insertState.Size - insertState.Position;
                Debug.Assert(insertionRemains >= 1);

                var length = Math.Min(insertionRemains, count);

                while (length >= 1)
                {
                    Debug.Assert(length <= byte.MaxValue);

                    if (this.deltaBufferIndex >= this.deltaBufferCount)
                    {
                        if (!await this.PrepareAsync(ct))
                        {
                            throw new InvalidDataException(
                                "Broken deltified stream: Step=8");
                        }
                    }

                    var l = Math.Min(
                        length, this.deltaBufferCount - this.deltaBufferIndex);

                    Array.Copy(
                        this.deltaBuffer, this.deltaBufferIndex,
                        buffer, offset, l);

                    offset += l;
                    count -= l;
                    read += l;
                    this.deltaBufferIndex += l;

                    insertState.Position += (byte)l;
                    length -= l;
                    insertionRemains -= l;
                }

                if (insertionRemains <= 0)
                {
                    this.state = null;
                }
            }
        }

        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer, int offset, int count, CancellationToken ct) =>
        this.ReadValueTaskAsync(buffer, offset, count, ct).AsTask();
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
        Stream baseObjectStream, Stream deltaStream,
        BufferPool pool, IFileSystem fileSystem,
        CancellationToken ct)
#else
    public static async Task<DeltaDecodedStream> CreateAsync(
        Stream baseObjectStream, Stream deltaStream,
        BufferPool pool, IFileSystem fileSystem,
        CancellationToken ct)
#endif
    {
        void Throw(int step) =>
            throw new InvalidDataException(
                $"Could not parse the object. Step={step}");

        var preloadIndex = 0;
        using var preloadBuffer = pool.Take(preloadBufferSize);

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
            await MemoizedStream.CreateAsync(baseObjectStream, (long)baseObjectLength, pool, fileSystem, ct),
            deltaStream,
            preloadBuffer.Detach(), preloadIndex, read, (long)decodedObjectLength);
    }
}
