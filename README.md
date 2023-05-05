# GitReader

Lightweight Git local repository traversal library.

![GitReader](Images/GitReader.100.png)

# Status

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

|Target|Pakcage|
|:----|:----|
|Any|[![NuGet GitReader](https://img.shields.io/nuget/v/GitReader.svg?style=flat)](https://www.nuget.org/packages/GitReader)|
|F# binding|[![NuGet FSharp.GitReader](https://img.shields.io/nuget/v/FSharp.GitReader.svg?style=flat)](https://www.nuget.org/packages/FSharp.GitReader)|

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

### Target .NET platforms

* .NET 7.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1 to 1.6
* .NET Framework 4.8.1 to 3.5

### F# specialized binding

F# 5.0 or upper, it contains F# friendly signature definition.

* .NET 7.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1, 2.0
* .NET Framework 4.8.1 to 4.6.1

Note: All target framework variations are tested only newest it.

----

## How to use

Install [GitReader](https://www.nuget.org/packages/GitReader) from NuGet.

* Install [FSharp.GitReader](https://www.nuget.org/packages/FSharp.GitReader) when you need to use with F#.
  It has F# friendly signature definition.

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
if (repository.GetHead() is Branch head)
{
    Console.WriteLine($"Name: {head.Name}");

    Console.WriteLine($"Hash: {head.Head.Hash}");
    Console.WriteLine($"Author: {head.Head.Author}");
    Console.WriteLine($"Committer: {head.Head.Committer}");
    Console.WriteLine($"Subject: {head.Head.Subject}");
    Console.WriteLine($"Body: {head.Head.Body}");
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
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
}
```

### Get a branch head commit

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Subject: {branch.Head.Subject}");
Console.WriteLine($"Body: {branch.Head.Body}");
```

### Get a remote branch head commit

```csharp
Branch branch = repository.RemoteBranches["origin/develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Subject: {branch.Head.Subject}");
Console.WriteLine($"Body: {branch.Head.Body}");
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
    Console.WriteLine($"Subject: {branch.Head.Subject}");
    Console.WriteLine($"Body: {branch.Head.Body}");
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
        Console.WriteLine($"Subject: {parent.Subject}");
        Console.WriteLine($"Body: {parent.Body}");
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
    Console.WriteLine($"Subject: {current.Subject}");
    Console.WriteLine($"Body: {current.Body}");

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

if (await repository.GetCurrentHeadReferenceAsync() is PrimitiveReference head)
{
    if (await repository.GetCommitAsync(head) is PrimitiveCommit commit)
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
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
{
    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Message: {commit.Message}");
}
```

### Read a branch head commit

```csharp
PrimitiveReference head = await repository.GetBranchHeadReferenceAsync("develop");

if (await repository.GetCommitAsync(head) is PrimitiveCommit commit)
{
    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Message: {commit.Message}");
}
```

### Enumerate branches

```csharp
PrimitiveReference[] branches = await repository.GetBranchHeadReferencesAsync();

foreach (PrimitiveReference branch in branches)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"Commit: {branch.Commit}");
}
```

### Enumerate tags

```csharp
PrimitiveReference[] tagReferences = await repository.GetTagReferencesAsync();

foreach (PrimitiveReference tagReference in tagReferences)
{
    PrimitiveTag tag = await repository.GetTagAsync(tagReference);

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
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
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
        if (await repository.GetCommitAsync(primary) is not PrimitiveCommit parent)
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

----

## License

Apache-v2

## History

* 0.7.0:
  * Switched primitive interface types with prefix `Primitive`.
  * Improved performance.
  * 
* 0.6.0:
  * Improved message handling on high-level interfaces.
  * Re-implemented delta compression decoder.
  * Supported both FETCH_HEAD and packed_refs parser.
  * Improved performance.
  * Removed index locker.
  * Fixed contains invalid hash on annotated commit tag.
  * Improved minor interface features.
* 0.5.0:
  * Supported deconstructor by F# active patterns.
  * Downgraded at least F# version 5.
* 0.4.0:
  * Added F# binding.
  * Fixed lack for head branch name.
* 0.3.0:
  * Supported ability for not found detection.
* 0.2.0:
  * The shape of the public interfaces are almost fixed.
  * Improved high-level interfaces.
  * Splitted core library (Preparation for F# binding)
* 0.1.0:
  * Initial release.
