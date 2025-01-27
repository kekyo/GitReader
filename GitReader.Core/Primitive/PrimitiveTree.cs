////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using System;
using System.Linq;

namespace GitReader.Primitive;

[Flags]
public enum PrimitiveModeFlags
{
    Tree = 0xa000,
    Blob = 0x8000,
    SubModule = 0x6000,
    Directory = 0x4000,
    SpecialMask = 0xf000,
    OwnerRead = 0x0100,
    OwnerWrite = 0x0080,
    OwnerExecute = 0x0040,
    GroupRead = 0x0020,
    GroupWrite = 0x0010,
    GroupExecute = 0x0008,
    OtherRead = 0x0004,
    OtherWrite = 0x0002,
    OtherExecute = 0x0001,
}

public enum PrimitiveSpecialModes
{
    Unknown,
    Blob,
    Directory,
    SubModule,
    Tree,
}

public readonly struct PrimitiveTreeEntry : IEquatable<PrimitiveTreeEntry>
{
    public readonly Hash Hash;
    public readonly string Name;
    public readonly PrimitiveModeFlags Modes;

    public PrimitiveTreeEntry(
        Hash hash,
        string name,
        PrimitiveModeFlags modes)
    {
        this.Hash = hash;
        this.Name = name;
        this.Modes = modes;
    }

    public PrimitiveSpecialModes SpecialModes =>
        (this.Modes & PrimitiveModeFlags.SpecialMask) switch
        {
            PrimitiveModeFlags.Directory => PrimitiveSpecialModes.Directory,
            PrimitiveModeFlags.Blob => PrimitiveSpecialModes.Blob,
            PrimitiveModeFlags.SubModule => PrimitiveSpecialModes.SubModule,
            PrimitiveModeFlags.Tree => PrimitiveSpecialModes.Tree,
            (PrimitiveModeFlags.Directory | PrimitiveModeFlags.Tree) => PrimitiveSpecialModes.SubModule,
            _ => PrimitiveSpecialModes.Unknown,
        };

    public bool Equals(PrimitiveTreeEntry rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Name.Equals(rhs.Name) &&
        this.Modes == rhs.Modes;

    bool IEquatable<PrimitiveTreeEntry>.Equals(PrimitiveTreeEntry rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveTreeEntry rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Modes.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Modes}: {this.Name}: {this.Hash}";

    public static implicit operator Hash(PrimitiveTreeEntry entry) =>
        entry.Hash;
}

public readonly struct PrimitiveTree : IEquatable<PrimitiveTree>
{
    public readonly Hash Hash;
    public readonly ReadOnlyArray<PrimitiveTreeEntry> Children;

    public PrimitiveTree(
        Hash hash,
        ReadOnlyArray<PrimitiveTreeEntry> children)
    {
        this.Hash = hash;
        this.Children = children;
    }

    public bool Equals(PrimitiveTree rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        (this.Children?.SequenceEqual(rhs.Children ?? Utilities.Empty<PrimitiveTreeEntry>()) ?? false);

    bool IEquatable<PrimitiveTree>.Equals(PrimitiveTree rhs) =>
        this.Equals(rhs);

    public override bool Equals(object? obj) =>
        obj is PrimitiveTree rhs && this.Equals(rhs);

    public override int GetHashCode() =>
        this.Children.Aggregate(
            this.Hash.GetHashCode(),
            (agg, v) =>
            {
                unchecked
                {
                    return (agg * 397) ^ v.GetHashCode();
                }
            });

    public override string ToString() =>
        $"{this.Hash}: Children={this.Children.Count}";

    public static implicit operator Hash(PrimitiveTree tree) =>
        tree.Hash;
}
