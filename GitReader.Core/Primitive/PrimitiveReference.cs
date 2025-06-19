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
/// Represents a primitive Git reference with name, path, and target hash.
/// </summary>
public readonly struct PrimitiveReference : IEquatable<PrimitiveReference>
{
    /// <summary>
    /// The name of the reference.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The relative path of the reference within the Git repository.
    /// </summary>
    public readonly string RelativePath;
    
    /// <summary>
    /// The target hash that this reference points to.
    /// </summary>
    public readonly Hash Target;

    /// <summary>
    /// Initializes a new instance of the PrimitiveReference struct.
    /// </summary>
    /// <param name="name">The name of the reference.</param>
    /// <param name="relativePath">The relative path of the reference.</param>
    /// <param name="target">The target hash.</param>
    public PrimitiveReference(
        string name,
        string relativePath,
        Hash target)
    {
        this.Name = name;
        this.RelativePath = relativePath;
        this.Target = target;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveReference is equal to the current PrimitiveReference.
    /// </summary>
    /// <param name="rhs">The PrimitiveReference to compare with the current PrimitiveReference.</param>
    /// <returns>true if the specified PrimitiveReference is equal to the current PrimitiveReference; otherwise, false.</returns>
    public bool Equals(PrimitiveReference rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.RelativePath.Equals(rhs.RelativePath) &&
        this.Target.Equals(rhs.Target);

    bool IEquatable<PrimitiveReference>.Equals(PrimitiveReference rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveReference.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveReference.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveReference; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveReference rhs && this.Equals(rhs);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Name.GetHashCode();
            hashCode = (hashCode * 397) ^ this.RelativePath.GetHashCode();
            hashCode = (hashCode * 397) ^ this.Target.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the primitive reference.
    /// </summary>
    /// <returns>A string representation of the primitive reference.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.Target}";

    /// <summary>
    /// Implicitly converts a PrimitiveReference to its target Hash.
    /// </summary>
    /// <param name="reference">The PrimitiveReference to convert.</param>
    /// <returns>The target Hash of the reference.</returns>
    public static implicit operator Hash(PrimitiveReference reference) =>
        reference.Target;
}
