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

/// <summary>
/// Specifies the primitive file mode flags in Git tree objects.
/// </summary>
[Flags]
public enum PrimitiveModeFlags
{
    /// <summary>
    /// Represents a tree object.
    /// </summary>
    Tree = 0xa000,
    
    /// <summary>
    /// Represents a blob (file) object.
    /// </summary>
    Blob = 0x8000,
    
    /// <summary>
    /// Represents a submodule object.
    /// </summary>
    SubModule = 0x6000,
    
    /// <summary>
    /// Represents a directory object.
    /// </summary>
    Directory = 0x4000,
    
    /// <summary>
    /// Mask for extracting special object type flags.
    /// </summary>
    SpecialMask = 0xf000,
    
    /// <summary>
    /// Owner has read permission.
    /// </summary>
    OwnerRead = 0x0100,
    
    /// <summary>
    /// Owner has write permission.
    /// </summary>
    OwnerWrite = 0x0080,
    
    /// <summary>
    /// Owner has execute permission.
    /// </summary>
    OwnerExecute = 0x0040,
    
    /// <summary>
    /// Group has read permission.
    /// </summary>
    GroupRead = 0x0020,
    
    /// <summary>
    /// Group has write permission.
    /// </summary>
    GroupWrite = 0x0010,
    
    /// <summary>
    /// Group has execute permission.
    /// </summary>
    GroupExecute = 0x0008,
    
    /// <summary>
    /// Others have read permission.
    /// </summary>
    OtherRead = 0x0004,
    
    /// <summary>
    /// Others have write permission.
    /// </summary>
    OtherWrite = 0x0002,
    
    /// <summary>
    /// Others have execute permission.
    /// </summary>
    OtherExecute = 0x0001,
}

/// <summary>
/// Specifies the special modes for primitive Git objects.
/// </summary>
public enum PrimitiveSpecialModes
{
    /// <summary>
    /// Unknown object type.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Blob (file) object.
    /// </summary>
    Blob,
    
    /// <summary>
    /// Directory object.
    /// </summary>
    Directory,
    
    /// <summary>
    /// Submodule object.
    /// </summary>
    SubModule,
    
    /// <summary>
    /// Tree object.
    /// </summary>
    Tree,
}

/// <summary>
/// Represents a primitive tree entry in Git objects.
/// </summary>
public readonly struct PrimitiveTreeEntry : IEquatable<PrimitiveTreeEntry>
{
    /// <summary>
    /// The hash identifier of this tree entry.
    /// </summary>
    public readonly Hash Hash;
    
    /// <summary>
    /// The name of this tree entry.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The primitive mode flags of this tree entry.
    /// </summary>
    public readonly PrimitiveModeFlags Modes;

    /// <summary>
    /// Initializes a new instance of the PrimitiveTreeEntry struct.
    /// </summary>
    /// <param name="hash">The hash of the tree entry.</param>
    /// <param name="name">The name of the tree entry.</param>
    /// <param name="modes">The mode flags of the tree entry.</param>
    public PrimitiveTreeEntry(
        Hash hash,
        string name,
        PrimitiveModeFlags modes)
    {
        this.Hash = hash;
        this.Name = name;
        this.Modes = modes;
    }

    /// <summary>
    /// Gets the special mode type of this tree entry.
    /// </summary>
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

    /// <summary>
    /// Determines whether the specified PrimitiveTreeEntry is equal to the current PrimitiveTreeEntry.
    /// </summary>
    /// <param name="rhs">The PrimitiveTreeEntry to compare with the current PrimitiveTreeEntry.</param>
    /// <returns>true if the specified PrimitiveTreeEntry is equal to the current PrimitiveTreeEntry; otherwise, false.</returns>
    public bool Equals(PrimitiveTreeEntry rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Name.Equals(rhs.Name) &&
        this.Modes == rhs.Modes;

    bool IEquatable<PrimitiveTreeEntry>.Equals(PrimitiveTreeEntry rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveTreeEntry.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveTreeEntry.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveTreeEntry; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveTreeEntry rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
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

    /// <summary>
    /// Returns a string representation of this primitive tree entry.
    /// </summary>
    /// <returns>A string representation of this primitive tree entry.</returns>
    public override string ToString() =>
        $"{this.Modes}: {this.Name}: {this.Hash}";

    /// <summary>
    /// Implicitly converts a PrimitiveTreeEntry to its Hash.
    /// </summary>
    /// <param name="entry">The entry to convert.</param>
    /// <returns>The hash of the entry.</returns>
    public static implicit operator Hash(PrimitiveTreeEntry entry) =>
        entry.Hash;
}

/// <summary>
/// Represents a primitive Git tree object.
/// </summary>
public readonly struct PrimitiveTree : IEquatable<PrimitiveTree>
{
    /// <summary>
    /// The hash identifier of this tree.
    /// </summary>
    public readonly Hash Hash;
    
    /// <summary>
    /// The children entries of this tree.
    /// </summary>
    public readonly ReadOnlyArray<PrimitiveTreeEntry> Children;

    /// <summary>
    /// Initializes a new instance of the PrimitiveTree struct.
    /// </summary>
    /// <param name="hash">The hash of the tree.</param>
    /// <param name="children">The children entries of the tree.</param>
    public PrimitiveTree(
        Hash hash,
        ReadOnlyArray<PrimitiveTreeEntry> children)
    {
        this.Hash = hash;
        this.Children = children;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveTree is equal to the current PrimitiveTree.
    /// </summary>
    /// <param name="rhs">The PrimitiveTree to compare with the current PrimitiveTree.</param>
    /// <returns>true if the specified PrimitiveTree is equal to the current PrimitiveTree; otherwise, false.</returns>
    public bool Equals(PrimitiveTree rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        (this.Children?.SequenceEqual(rhs.Children ?? Utilities.Empty<PrimitiveTreeEntry>()) ?? false);

    bool IEquatable<PrimitiveTree>.Equals(PrimitiveTree rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveTree.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveTree.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveTree; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveTree rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
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

    /// <summary>
    /// Returns a string representation of this primitive tree.
    /// </summary>
    /// <returns>A string representation of this primitive tree.</returns>
    public override string ToString() =>
        $"{this.Hash}: Children={this.Children.Count}";

    /// <summary>
    /// Implicitly converts a PrimitiveTree to its Hash.
    /// </summary>
    /// <param name="tree">The tree to convert.</param>
    /// <returns>The hash of the tree.</returns>
    public static implicit operator Hash(PrimitiveTree tree) =>
        tree.Hash;
}
