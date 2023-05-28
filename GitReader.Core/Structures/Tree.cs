////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

namespace GitReader.Structures;

// Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0659

[Flags]
public enum ModeFlags
{
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

public abstract class Tree : IEquatable<Tree>
{
    public readonly Hash Hash;

    private protected Tree(
        Hash hash) =>
        this.Hash = hash;

    public abstract bool Equals(Tree rhs);

    bool IEquatable<Tree>.Equals(Tree? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Tree rhs && this.Equals(rhs);

    public static implicit operator Hash(Tree tree) =>
        tree.Hash;
}

public abstract class TreeEntry : Tree
{
    public readonly string Name;
    public readonly ModeFlags Modes;

    private protected TreeEntry(
        Hash hash,
        string name,
        ModeFlags modes) :
        base(hash)
    {
        this.Name = name;
        this.Modes = modes;
    }
}

public sealed class TreeBlobEntry :
    TreeEntry, IRepositoryReference
{
    private readonly WeakReference rwr;

    internal TreeBlobEntry(
        WeakReference rwr,
        Hash hash,
        string name,
        ModeFlags modes) :
        base(hash, name, modes) =>
        this.rwr = rwr;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    public override bool Equals(Tree rhs) =>
        rhs is TreeBlobEntry r &&
        this.Hash.Equals(r.Hash) &&
        this.Name.Equals(r.Name) &&
        this.Modes == r.Modes;

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
        $"File: {this.Modes}: {this.Name}: {this.Hash}";
}

public sealed class TreeDirectoryEntry : TreeEntry
{
    public readonly TreeEntry[] Children;

    internal TreeDirectoryEntry(
        Hash hash,
        string name,
        ModeFlags modes,
        TreeEntry[] children) :
        base(hash, name, modes) =>
        this.Children = children;

    public override bool Equals(Tree rhs) =>
        rhs is TreeDirectoryEntry r &&
        this.Hash.Equals(r.Hash) &&
        this.Name.Equals(r.Name) &&
        this.Modes == r.Modes &&
        this.Children.SequenceEqual(r.Children);

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

    public override string ToString() =>
        $"Directory: {this.Modes}: {this.Name}: {this.Hash}";
}

public sealed class TreeRoot : Tree
{
    public readonly TreeEntry[] Children;

    internal TreeRoot(
        Hash hash, TreeEntry[] children) :
        base(hash) =>
        this.Children = children;

    public override bool Equals(Tree rhs) =>
        rhs is TreeRoot r &&
        this.Hash.Equals(r.Hash) &&
        this.Children.SequenceEqual(r.Children);

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

    public override string ToString() =>
        $"{this.Hash}: Children={this.Children.Length}";
}
