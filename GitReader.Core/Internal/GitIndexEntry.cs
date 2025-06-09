////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Internal;

internal readonly struct GitIndexEntry
{
    public readonly uint CreationTime;
    public readonly uint CreationTimeNanoSecond;
    public readonly uint ModificationTime;
    public readonly uint ModificationTimeNanoSecond;
    public readonly uint Device;
    public readonly uint Inode;
    public readonly uint Mode;
    public readonly uint UserId;
    public readonly uint GroupId;
    public readonly uint FileSize;
    public readonly Hash ObjectHash;
    public readonly ushort Flags;
    public readonly string Path;

    public GitIndexEntry(
        uint creationTime,
        uint creationTimeNanoSecond,
        uint modificationTime,
        uint modificationTimeNanoSecond,
        uint device,
        uint inode,
        uint mode,
        uint userId,
        uint groupId,
        uint fileSize,
        Hash objectHash,
        ushort flags,
        string path)
    {
        this.CreationTime = creationTime;
        this.CreationTimeNanoSecond = creationTimeNanoSecond;
        this.ModificationTime = modificationTime;
        this.ModificationTimeNanoSecond = modificationTimeNanoSecond;
        this.Device = device;
        this.Inode = inode;
        this.Mode = mode;
        this.UserId = userId;
        this.GroupId = groupId;
        this.FileSize = fileSize;
        this.ObjectHash = objectHash;
        this.Flags = flags;
        this.Path = path;
    }

    public bool IsStageFlag => (this.Flags & 0x3000) != 0;

    public int StageNumber => (this.Flags & 0x3000) >> 12;

    public bool IsValidFlag => (this.Flags & 0x8000) == 0;

    public override string ToString() =>
        $"{this.Path}: {this.ObjectHash}";
} 