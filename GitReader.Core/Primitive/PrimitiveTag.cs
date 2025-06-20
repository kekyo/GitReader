////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;

namespace GitReader.Primitive;

/// <summary>
/// Represents a primitive Git tag with its basic properties.
/// </summary>
public readonly struct PrimitiveTag : IEquatable<PrimitiveTag>
{
    /// <summary>
    /// The hash of the tagged object.
    /// </summary>
    public readonly Hash Hash;
    
    /// <summary>
    /// The type of the tagged object.
    /// </summary>
    public readonly ObjectTypes Type;
    
    /// <summary>
    /// The name of the tag.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The tagger signature, or null for lightweight tags.
    /// </summary>
    public readonly Signature? Tagger;
    
    /// <summary>
    /// The tag message, or null for lightweight tags.
    /// </summary>
    public readonly string? Message;

    /// <summary>
    /// Initializes a new instance of the PrimitiveTag struct.
    /// </summary>
    /// <param name="hash">The hash of the tagged object.</param>
    /// <param name="type">The type of the tagged object.</param>
    /// <param name="name">The name of the tag.</param>
    /// <param name="tagger">The tagger signature, or null for lightweight tags.</param>
    /// <param name="message">The tag message, or null for lightweight tags.</param>
    public PrimitiveTag(
        Hash hash,
        ObjectTypes type,
        string name,
        Signature? tagger,
        string? message)
    {
        this.Hash = hash;
        this.Type = type;
        this.Name = name;
        this.Tagger = tagger;
        this.Message = message;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveTag is equal to the current PrimitiveTag.
    /// </summary>
    /// <param name="rhs">The PrimitiveTag to compare with the current PrimitiveTag.</param>
    /// <returns>true if the specified PrimitiveTag is equal to the current PrimitiveTag; otherwise, false.</returns>
    public bool Equals(PrimitiveTag rhs) =>
        this.Hash.Equals(rhs.Hash) &&
        this.Type.Equals(rhs.Type) &&
        this.Name.Equals(rhs.Name) &&
        this.Tagger.Equals(rhs.Tagger) &&
        this.Message == rhs.Message;

    bool IEquatable<PrimitiveTag>.Equals(PrimitiveTag rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveTag.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveTag.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveTag; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveTag rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Tagger.GetHashCode();
            hashCode = (hashCode * 397) ^ (this.Message?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the primitive tag.
    /// </summary>
    /// <returns>A string representation of the primitive tag.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.Type}: {this.Hash}";

    /// <summary>
    /// Implicitly converts a PrimitiveTag to its Hash.
    /// </summary>
    /// <param name="tag">The PrimitiveTag to convert.</param>
    /// <returns>The Hash of the tagged object.</returns>
    public static implicit operator Hash(PrimitiveTag tag) =>
        tag.Hash;
}
