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
/// Represents a primitive Git tag reference with name, path, and target hashes.
/// </summary>
public readonly struct PrimitiveTagReference :
    IEquatable<PrimitiveTagReference>
{
    /// <summary>
    /// The name of the tag reference.
    /// </summary>
    public readonly string Name;
    
    /// <summary>
    /// The relative path of the tag reference within the Git repository.
    /// </summary>
    public readonly string RelativePath;
    
    /// <summary>
    /// The hash of the object or commit that this tag points to.
    /// </summary>
    public readonly Hash ObjectOrCommitHash;
    
    /// <summary>
    /// The commit hash if this is an annotated tag, or null for lightweight tags.
    /// </summary>
    public readonly Hash? CommitHash;

    /// <summary>
    /// Initializes a new instance of the PrimitiveTagReference struct.
    /// </summary>
    /// <param name="name">The name of the tag reference.</param>
    /// <param name="relativePath">The relative path of the tag reference.</param>
    /// <param name="objectOrCommitHash">The hash of the object or commit that this tag points to.</param>
    /// <param name="commitHash">The commit hash if this is an annotated tag, or null for lightweight tags.</param>
    public PrimitiveTagReference(
        string name,
        string relativePath,
        Hash objectOrCommitHash,
        Hash? commitHash)
    {
        this.Name = name;
        this.RelativePath = relativePath;
        this.ObjectOrCommitHash = objectOrCommitHash;
        this.CommitHash = commitHash;
    }

    /// <summary>
    /// Determines whether the specified PrimitiveTagReference is equal to the current PrimitiveTagReference.
    /// </summary>
    /// <param name="rhs">The PrimitiveTagReference to compare with the current PrimitiveTagReference.</param>
    /// <returns>true if the specified PrimitiveTagReference is equal to the current PrimitiveTagReference; otherwise, false.</returns>
    public bool Equals(PrimitiveTagReference rhs) =>
        this.Name.Equals(rhs.Name) &&
        this.RelativePath.Equals(rhs.RelativePath) &&
        this.ObjectOrCommitHash.Equals(rhs.ObjectOrCommitHash) &&
        this.CommitHash.Equals(rhs.CommitHash);

    bool IEquatable<PrimitiveTagReference>.Equals(PrimitiveTagReference rhs) =>
        this.Equals(rhs);

    /// <summary>
    /// Determines whether the specified object is equal to the current PrimitiveTagReference.
    /// </summary>
    /// <param name="obj">The object to compare with the current PrimitiveTagReference.</param>
    /// <returns>true if the specified object is equal to the current PrimitiveTagReference; otherwise, false.</returns>
    public override bool Equals(object? obj) =>
        obj is PrimitiveTagReference rhs && this.Equals(rhs);

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
            hashCode = (hashCode * 397) ^ this.ObjectOrCommitHash.GetHashCode();
            hashCode = (hashCode * 397) ^ this.CommitHash.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Returns a string representation of the primitive tag reference.
    /// </summary>
    /// <returns>A string representation of the primitive tag reference.</returns>
    public override string ToString() =>
        $"{this.Name}: {this.ObjectOrCommitHash}{(this.CommitHash is { } ct ? $" [{ct}]" : "")}";
}
