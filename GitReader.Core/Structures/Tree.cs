////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

namespace GitReader.Structures;

// Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0659

/// <summary>
/// Specifies file mode flags in Git tree objects.
/// </summary>
[Flags]
public enum ModeFlags
{
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
/// Represents a base class for Git tree objects.
/// </summary>
public abstract class Tree : IEquatable<Tree>
{
    /// <summary>
    /// The hash identifier of this tree object.
    /// </summary>
    public readonly Hash Hash;

    private protected Tree(
        Hash hash) =>
        this.Hash = hash;

    /// <summary>
    /// Determines whether the specified Tree is equal to the current Tree.
    /// </summary>
    /// <param name="rhs">The Tree to compare with the current Tree.</param>
    /// <returns>true if the specified Tree is equal to the current Tree; otherwise, false.</returns>
    public abstract bool Equals(Tree rhs);

    bool IEquatable<Tree>.Equals(Tree? rhs) =>
        this.Equals(rhs!);

    /// <summary>
    /// Determines whether the specified object is equal to the current Tree.
    /// </summary>
    /// <param name="obj">The object to compare with the current Tree.</param>
    /// <returns>true if the specified object is equal to the current Tree; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Tree rhs && this.Equals(rhs);

    /// <summary>
    /// Implicitly converts a Tree to its Hash.
    /// </summary>
    /// <param name="tree">The tree to convert.</param>
    /// <returns>The hash of the tree.</returns>
    public static implicit operator Hash(Tree tree) =>
        tree.Hash;
}

/// <summary>
/// Represents a base class for Git tree entries.
/// </summary>
public abstract class TreeEntry : Tree
{
    /// <summary>
    /// The name of this tree entry.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The file mode flags of this tree entry.
    /// </summary>
    public readonly ModeFlags Modes;
    
    /// <summary>
    /// The parent tree of this entry.
    /// </summary>
    public readonly Tree Parent;

    private protected TreeEntry(
        Hash hash,
        string name,
        ModeFlags modes,
        Tree parent) :
        base(hash)
    {
        this.Name = name;
        this.Modes = modes;
        this.Parent = parent;
    }
}

/// <summary>
/// Represents a blob (file) entry in a Git tree.
/// </summary>
public sealed class TreeBlobEntry :
    TreeEntry, IRepositoryReference
{
    private readonly WeakReference rwr;

    internal TreeBlobEntry(
        WeakReference rwr,
        Hash hash,
        string name,
        ModeFlags modes,
        Tree parent) :
        base(hash, name, modes, parent) =>
        this.rwr = rwr;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    /// <summary>
    /// Determines whether the specified Tree is equal to the current TreeBlobEntry.
    /// </summary>
    /// <param name="rhs">The Tree to compare with the current TreeBlobEntry.</param>
    /// <returns>true if the specified Tree is equal to the current TreeBlobEntry; otherwise, false.</returns>
    public override bool Equals(Tree rhs) =>
        rhs is TreeBlobEntry r &&
        this.Hash.Equals(r.Hash) &&
        this.Name.Equals(r.Name) &&
        this.Modes == r.Modes;

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
    /// Returns a string representation of this tree blob entry.
    /// </summary>
    /// <returns>A string representation of this tree blob entry.</returns>
    public override string ToString() =>
        $"File: {this.Modes}: {this.Name}: {this.Hash}";
}

/// <summary>
/// Represents a tree entry that can contain child entries.
/// </summary>
public interface IParentTreeEntry
{
    /// <summary>
    /// Gets the child entries of this tree entry.
    /// </summary>
    TreeEntry[] Children { get; }
}

/// <summary>
/// Represents a directory entry in a Git tree.
/// </summary>
public sealed class TreeDirectoryEntry :
    TreeEntry, IParentTreeEntry
{
    internal TreeDirectoryEntry(
        Hash hash,
        string name,
        ModeFlags modes,
        Tree parent) :
        base(hash, name, modes, parent)
    {
    }

    /// <summary>
    /// Gets the child entries of this directory.
    /// </summary>
    public TreeEntry[] Children { get; private set; } = null!;

    internal void SetChildren(TreeEntry[] children) =>
        this.Children = children;

    /// <summary>
    /// Determines whether the specified Tree is equal to the current TreeDirectoryEntry.
    /// </summary>
    /// <param name="rhs">The Tree to compare with the current TreeDirectoryEntry.</param>
    /// <returns>true if the specified Tree is equal to the current TreeDirectoryEntry; otherwise, false.</returns>
    public override bool Equals(Tree rhs) =>
        rhs is TreeDirectoryEntry r &&
        this.Hash.Equals(r.Hash) &&
        this.Name.Equals(r.Name) &&
        this.Modes == r.Modes &&
        this.Children.SequenceEqual(r.Children);

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
            return this.Children.Aggregate(
                hashCode,
                (agg, v) =>
                {
                    unchecked
                    {
                        return (agg * 397) ^ v.GetHashCode();
                    }
                });
        }
    }

    /// <summary>
    /// Returns a string representation of this tree directory entry.
    /// </summary>
    /// <returns>A string representation of this tree directory entry.</returns>
    public override string ToString() =>
        $"Directory: {this.Modes}: {this.Name}: {this.Hash}";
}

/// <summary>
/// Represents a Git submodule entry in a tree.
/// </summary>
public sealed class TreeSubModuleEntry :
    TreeEntry, IRepositoryReference
{
    private readonly WeakReference rwr;

    internal TreeSubModuleEntry(
        WeakReference rwr,
        Hash hash,
        string name,
        ModeFlags modes,
        Tree parent) :
        base(hash, name, modes, parent) =>
        this.rwr = rwr;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    /// <summary>
    /// Determines whether the specified Tree is equal to the current TreeSubModuleEntry.
    /// </summary>
    /// <param name="rhs">The Tree to compare with the current TreeSubModuleEntry.</param>
    /// <returns>true if the specified Tree is equal to the current TreeSubModuleEntry; otherwise, false.</returns>
    public override bool Equals(Tree rhs) =>
        rhs is TreeSubModuleEntry r &&
        this.Hash.Equals(r.Hash) &&
        this.Name.Equals(r.Name) &&
        this.Modes == r.Modes;

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
    /// Returns a string representation of this tree submodule entry.
    /// </summary>
    /// <returns>A string representation of this tree submodule entry.</returns>
    public override string ToString() =>
        $"SubModule: {this.Name}: {this.Hash}";
}

/// <summary>
/// Represents the root of a Git tree structure.
/// </summary>
public sealed class TreeRoot :
    Tree, IParentTreeEntry
{
    internal TreeRoot(Hash hash) :
        base(hash)
    {
    }

    /// <summary>
    /// Gets the child entries of this tree root.
    /// </summary>
    public TreeEntry[] Children { get; private set; } = null!;

    internal void SetChildren(TreeEntry[] children) =>
        this.Children = children;

    /// <summary>
    /// Determines whether the specified Tree is equal to the current TreeRoot.
    /// </summary>
    /// <param name="rhs">The Tree to compare with the current TreeRoot.</param>
    /// <returns>true if the specified Tree is equal to the current TreeRoot; otherwise, false.</returns>
    public override bool Equals(Tree rhs) =>
        rhs is TreeRoot r &&
        this.Hash.Equals(r.Hash) &&
        this.Children.SequenceEqual(r.Children);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            return this.Children.Aggregate(
                hashCode,
                (agg, v) =>
                {
                    unchecked
                    {
                        return (agg * 397) ^ v.GetHashCode();
                    }
                });
        }
    }

    /// <summary>
    /// Returns a string representation of this tree root.
    /// </summary>
    /// <returns>A string representation of this tree root.</returns>
    public override string ToString() =>
        $"Root: {this.Hash}: Children={this.Children.Length}";
}
