////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;

namespace GitReader.Structures;

public sealed class Tag :
    IEquatable<Tag>, IInternalCommitReference
{
    private readonly WeakReference rwr;
    internal Annotation? annotation;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash? TagHash;

    public readonly ObjectTypes Type;
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash ObjectHash;

    public readonly string Name;

    internal Tag(
        WeakReference rwr, Hash? tagHash, ObjectTypes type,
        Hash objectHash, string name, Annotation? annotation)
    {
        this.rwr = rwr;
        this.TagHash = tagHash;
        this.Type = type;
        this.ObjectHash = objectHash;
        this.Name = name;
        this.annotation = annotation;
    }

    WeakReference IRepositoryReference.Repository =>
        this.rwr;

    Hash ICommitReference.Hash =>
        this.ObjectHash;

    public bool HasAnnotation =>
        this.TagHash.HasValue;

    public bool Equals(Tag rhs) =>
        rhs is { } &&
        this.TagHash.Equals(rhs.TagHash) &&
        this.Type.Equals(rhs.Type) &&
        this.ObjectHash.Equals(rhs.ObjectHash) &&
        this.Name.Equals(rhs.Name) &&
        this.annotation is { } la &&
        rhs.annotation is { } ra &&
        la.Equals(ra);

    bool IEquatable<Tag>.Equals(Tag? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Tag rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.TagHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ this.ObjectHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.annotation?.GetHashCode() ?? 0;
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Name}: {this.Type}: {(this.ObjectHash is { } oh ? $" [{oh}]" : "")}";
}

public sealed class Annotation : IEquatable<Annotation>
{
    public readonly Signature? Tagger;
    public readonly string? Message;

    internal Annotation(
        Signature? tagger, string? message)
    {
        this.Tagger = tagger;
        this.Message = message;
    }

    public bool Equals(Annotation rhs) =>
        rhs is { } &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Annotation>.Equals(Annotation? rhs) =>
        this.Equals(rhs!);

    public override bool Equals(object? obj) =>
        obj is Annotation rhs && this.Equals(rhs);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    public override string ToString() =>
        $"{this.Tagger}: {this.Message}";
}
