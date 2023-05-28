////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace GitReader.Structures;

public abstract class Tag : IEquatable<Tag>
{
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash Hash;

    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    private protected Tag(
        Hash hash, string name, Signature? tagger, string? message)
    {
        this.Hash = hash;
        this.Name = name;
        this.Tagger = tagger;
        this.Message = message;
    }

    public abstract ObjectTypes Type { get; }

    public bool Equals(Tag rhs) =>
        rhs is { } &&
        this.Hash.Equals(rhs.Hash) &&
        this.Name.Equals(rhs.Name) &&
        this.Type == rhs.Type &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Tag>.Equals(Tag? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Tag rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.Hash}";
}

public sealed class CommitTag :
    Tag, IInternalCommitReference
{
    internal readonly WeakReference rwr;

    internal CommitTag(
        WeakReference rwr, Hash hash, string name, Signature? tagger, string? message) :
        base(hash, name, tagger, message) =>
        this.rwr = rwr;

    public override ObjectTypes Type =>
        ObjectTypes.Commit;

    Hash ICommitReference.Hash =>
        this.Hash;

    WeakReference IRepositoryReference.Repository =>
        this.rwr;
}
