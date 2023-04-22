# GitReader

Lightweight Git local repository traversal library.

![GitReader](Images/GitReader.100.png)

# Status

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

[![NuGet GitReader](https://img.shields.io/nuget/v/GitReader.svg?style=flat)](https://www.nuget.org/packages/GitReader)

## What is this?

GitReader is a fully-managed Git local repository traversal library for a wide range of .NET environments.
It is lightweight, has a concise, easy-to-use interface, does not depend any other libraries, and does not contain native libraries,
making it suitable for any environment.

It has the following features:

* It provides information on Git branches, tags, and commits.
* Branch tree traversal.
* Read only interface makes immutability.
* Primitive and high-level interfaces ready.
* Fully asynchronous operation.
* Only contains 100% managed code. Independent of any external libraries other than the BCL and its compliant libraries.
* Reliable zlib decompression using the .NET standard deflate implementation.

This library was designed from the ground up to replace `libgit2sharp`, on which [RelaxVersioner](https://github.com/kekyo/CenterCLR.RelaxVersioner) depended.
It primarily fits the purpose of easily extracting commit information from a Git repository.

Target .NET platforms are (Almost all!):

* .NET 7.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1 to 1.6
* .NET Framework 4.8.1 to 3.5

----

## How to use

Install [GitReader](https://www.nuget.org/packages/GitReader) from NuGet.

GitReader has high-level interfaces and primitive interfaces.

* The high-level interface is an interface that abstracts the Git repository,
  making it easy to explore branches, tags and commits.
* The primitive interface is an interface that exposes the internal structure of the Git repository as it is,
  It is easy to handle and will be make high-performance with asynchronous operations
  if you know the structure knowledge.
  
----

## Samples (High-level interfaces)

### Get current head commit

```csharp
using GitReader;
using GitReader.Structures;

using Repository repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

// Found current head
if (repository.Head is { } head)
{
    Console.WriteLine($"Hash: {head.Hash}");
    Console.WriteLine($"Author: {head.Author}");
    Console.WriteLine($"Committer: {head.Committer}");
    Console.WriteLine($"Message: {head.Message}");
}
```

### Get a commit directly

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Message: {commit.Message}");
}
```

### Get a branch head commit

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Message: {branch.Head.Message}");
```

### Get a remote branch head commit

```csharp
Branch branch = repository.RemoteBranches["origin/develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Message: {branch.Head.Message}");
```

### Get a tag

```csharp
Tag tag = repository.Tags["1.2.3"];

Console.WriteLine($"Name: {tag.Name}");

Console.WriteLine($"Hash: {tag.Hash}");
Console.WriteLine($"Author: {tag.Author}");
Console.WriteLine($"Committer: {tag.Committer}");
Console.WriteLine($"Message: {tag.Message}");
```

### Get related branches and tags from a commit

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    Branch[] branches = commit.Branches;
    Branch[] remoteBranches = commit.RemoteBranches;
    Tags[] tags = commit.Tags;

    // ...
}
```

### Enumerate branches

```csharp
foreach (Branch branch in repository.Branches.Values)
{
    Console.WriteLine($"Name: {branch.Name}");

    Console.WriteLine($"Hash: {branch.Head.Hash}");
    Console.WriteLine($"Author: {branch.Head.Author}");
    Console.WriteLine($"Committer: {branch.Head.Committer}");
    Console.WriteLine($"Message: {branch.Head.Message}");
}
```

### Enumerate tags

```csharp
foreach (Tag tag in repository.Tags.Values)
{
    Console.WriteLine($"Name: {tag.Name}");

    Console.WriteLine($"Hash: {tag.Hash}");
    Console.WriteLine($"Author: {tag.Author}");
    Console.WriteLine($"Committer: {tag.Committer}");
    Console.WriteLine($"Message: {tag.Message}");
}
```

### Get parent commits

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Command commit)
{
    Commit[] parents = await commit.GetParentCommitsAsync();

    foreach (Commit parent in parents)
    {
        Console.WriteLine($"Hash: {parent.Hash}");
        Console.WriteLine($"Author: {parent.Author}");
        Console.WriteLine($"Committer: {parent.Committer}");
        Console.WriteLine($"Message: {parent.Message}");
    }
}
```

### Traverse a branch through primary commits

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");

Commit? current = branch.Head;

while (current != null)
{
    Console.WriteLine($"Hash: {current.Hash}");
    Console.WriteLine($"Author: {current.Author}");
    Console.WriteLine($"Committer: {current.Committer}");
    Console.WriteLine($"Message: {current.Message}");

    // Get primary parent commit.
    current = await current.GetPrimaryParentCommitAsync();
}
```

----

## Samples (Primitive interfaces)

### Read current head commit

```csharp
using GitReader;
using GitReader.Primitive;

using Repository repository =
    await Repository.Factory.OpenPrimitiveAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

if (await repository.GetCurrentHeadReferenceAsync() is Reference head)
{
    if (await repository.GetCommitAsync(head) is Commit commit)
    {
        Console.WriteLine($"Hash: {commit.Hash}");
        Console.WriteLine($"Author: {commit.Author}");
        Console.WriteLine($"Committer: {commit.Committer}");
        Console.WriteLine($"Message: {commit.Message}");
    }
}
```

### Read a commit directly

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Message: {commit.Message}");
}
```

### Read a branch head commit

```csharp
Reference head = await repository.GetBranchHeadReferenceAsync("develop");

if (await repository.GetCommitAsync(head) is Commit commit)
{
    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Message: {commit.Message}");
}
```

### Enumerate branches

```csharp
Reference[] branches = await repository.GetBranchHeadReferencesAsync();

foreach (Reference branch in branches)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"Commit: {branch.Commit}");
}
```

### Enumerate tags

```csharp
Reference[] tagReferences = await repository.GetTagReferencesAsync();

foreach (Reference tagReference in tagReferences)
{
    Tag tag = await repository.GetTagAsync(tagReference);

    Console.WriteLine($"Hash: {tag.Hash}");
    Console.WriteLine($"Type: {tag.Type}");
    Console.WriteLine($"Name: {tag.Name}");
    Console.WriteLine($"Tagger: {tag.Tagger}");
    Console.WriteLine($"Message: {tag.Message}");
}
```

### Traverse a commit through primary commits

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    while (true)
    {
        Console.WriteLine($"Hash: {commit.Hash}");
        Console.WriteLine($"Author: {commit.Author}");
        Console.WriteLine($"Committer: {commit.Committer}");
        Console.WriteLine($"Message: {commit.Message}");

        // Bottom of branch.
        if (commit.Parents.Length == 0)
        {
            break;
        }

        // Get primary parent.
        Hash primary = commit.Parents[0];
        if (await repository.GetCommitAsync(primary) is not Commit parent)
        {
            throw new Exception();
        }

        current = parent;
    }
}
```

----

## TODO

* Supported tree/file accessors.
* Supported CRC32 verifier.
* Supported F# bindings.

----

## License

Apache-v2

## History

* 0.2.0:
  * The shape of the public interfaces are almost fixed.
  * Improved high-level interfaces.
  * Splitted core library (Preparation for F# binding)
* 0.1.0:
  * Initial release.
