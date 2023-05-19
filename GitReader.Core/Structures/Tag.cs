////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Structures;

public abstract class Tag : CommitRef, IEquatable<Tag>
{
    public readonly string Name;
    public readonly Signature? Tagger;
    public readonly string? Message;

    private protected Tag(WeakReference rwr, Hash commit, string name, Signature? tagger, string? message)
     : base(rwr, commit)
    {
        this.Name = name;
        this.Tagger = tagger;
        this.Message = message;
    }

    public abstract ObjectTypes Type { get; }

    public bool Equals(Tag rhs) =>
        rhs is { } &&
        this.CommitHash.Equals(rhs.CommitHash) &&
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
            var hashCode = this.CommitHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.CommitHash}";
}

public sealed class CommitTag : Tag
{

    internal CommitTag(WeakReference rwr, Hash hash, string name, Signature? tagger, string? message) :
        base(rwr, hash, name, tagger, message)
    {
    }

    public override ObjectTypes Type =>
        ObjectTypes.Commit;
}
