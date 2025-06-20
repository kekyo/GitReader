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

/// <summary>
/// Represents a Git tag reference.
/// </summary>
public sealed class Tag :
    IEquatable<Tag>, IInternalCommitReference
{
    private readonly WeakReference rwr;
    internal Annotation? annotation;

    /// <summary>
    /// The hash of the tag object itself (for annotated tags). Null for lightweight tags.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash? TagHash;

    /// <summary>
    /// The type of the object that this tag points to.
    /// </summary>
    public readonly ObjectTypes Type;
    
    /// <summary>
    /// The hash of the object that this tag points to.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public readonly Hash ObjectHash;

    /// <summary>
    /// The name of the tag.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Initializes a new instance of the Tag class.
    /// </summary>
    /// <param name="rwr">Weak reference to the repository.</param>
    /// <param name="tagHash">The hash of the tag object (for annotated tags).</param>
    /// <param name="type">The type of the object that this tag points to.</param>
    /// <param name="objectHash">The hash of the object that this tag points to.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="annotation">The annotation for annotated tags.</param>
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

    /// <summary>
    /// Gets a value indicating whether this tag has an annotation (i.e., it's an annotated tag).
    /// </summary>
    public bool HasAnnotation =>
        this.TagHash.HasValue;

    /// <summary>
    /// Determines whether the specified Tag is equal to the current Tag.
    /// </summary>
    /// <param name="rhs">The Tag to compare with the current Tag.</param>
    /// <returns>true if the specified Tag is equal to the current Tag; otherwise, false.</returns>
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

    /// <summary>
    /// Determines whether the specified object is equal to the current Tag.
    /// </summary>
    /// <param name="obj">The object to compare with the current Tag.</param>
    /// <returns>true if the specified object is equal to the current Tag; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Tag rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
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

    /// <summary>
    /// Returns a string representation of the tag.
    /// </summary>
    /// <returns>A string representation of the tag.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.Type}: {(this.ObjectHash is { } oh ? $" [{oh}]" : "")}";
}

/// <summary>
/// Represents the annotation information for annotated Git tags.
/// </summary>
public sealed class Annotation : IEquatable<Annotation>
{
    /// <summary>
    /// The signature of the person who created the tag.
    /// </summary>
    public readonly Signature? Tagger;
    
    /// <summary>
    /// The tag message.
    /// </summary>
    public readonly string? Message;

    /// <summary>
    /// Initializes a new instance of the Annotation class.
    /// </summary>
    /// <param name="tagger">The signature of the person who created the tag.</param>
    /// <param name="message">The tag message.</param>
    internal Annotation(
        Signature? tagger, string? message)
    {
        this.Tagger = tagger;
        this.Message = message;
    }

    /// <summary>
    /// Determines whether the specified Annotation is equal to the current Annotation.
    /// </summary>
    /// <param name="rhs">The Annotation to compare with the current Annotation.</param>
    /// <returns>true if the specified Annotation is equal to the current Annotation; otherwise, false.</returns>
    public bool Equals(Annotation rhs) =>
        rhs is { } &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<Annotation>.Equals(Annotation? rhs) =>
        this.Equals(rhs!);

    /// <summary>
    /// Determines whether the specified object is equal to the current Annotation.
    /// </summary>
    /// <param name="obj">The object to compare with the current Annotation.</param>
    /// <returns>true if the specified object is equal to the current Annotation; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is Annotation rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the annotation.
    /// </summary>
    /// <returns>A string representation of the annotation.</returns>
    public override string ToString() =>
        $"{this.Tagger}: {this.Message}";
}
