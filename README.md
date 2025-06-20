# GitReader

Lightweight Git local repository traversal library.

![GitReader](Images/GitReader.100.png)

# Status

[![Project Status: Active – The project has reached a stable, usable state and is being actively developed.](https://www.repostatus.org/badges/latest/active.svg)](https://www.repostatus.org/#active)

|Target|Pakcage|
|:----|:----|
|Any|[![NuGet GitReader](https://img.shields.io/nuget/v/GitReader.svg?style=flat)](https://www.nuget.org/packages/GitReader)|
|F# binding|[![NuGet FSharp.GitReader](https://img.shields.io/nuget/v/FSharp.GitReader.svg?style=flat)](https://www.nuget.org/packages/FSharp.GitReader)|

----

[![Japanese language](Images/Japanese.256.png)](https://github.com/kekyo/GitReader/blob/main/README_ja.md)

## What is this?

Have you ever wanted to access information about your local Git repository in .NET?
Explore and tag branches, get commit dates and contributor information, and read commit directory structures and files.

GitReader is written only managed code Git local repository traversal library for a wide range of .NET environments.
It is lightweight, has a concise, easy-to-use interface, does not depend any other libraries, and does not contain native libraries,
making it suitable for any environment.

Example:

```csharp
using GitReader;
using GitReader.Structures;

// Open repository (With high-level interface)
using var repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

// Found current head.
if (repository.Head is { } head)
{
    Console.WriteLine($"Name: {head.Name}");

    // Get head commit.
    var commit = await head.GetHeadCommitAsync();

    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
}
```

It has the following features:

* It provides information on Git branches, tags, and commits.
* Branch tree traversal.
* Read only interface makes immutability.
* Both high-level and primitive interfaces ready.
* Fully asynchronous operation without any sync-over-async implementation.
* Only contains 100% managed code. Independent of any external libraries other than the BCL and its compliant libraries.
* Reliable zlib decompression using the .NET standard deflate implementation.

This library was designed from the ground up to replace `libgit2sharp`, on which [RelaxVersioner](https://github.com/kekyo/CenterCLR.RelaxVersioner) depended.
It primarily fits the purpose of easily extracting commit information from a Git repository.

### Target .NET platforms

* .NET 9.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1 to 1.6
* .NET Framework 4.8.1 to 3.5

### F# specialized binding

F# 5.0 or upper, it contains F# friendly signature definition.
(Asynchronous operations with `Async` type, elimination of null values with `Option`, etc.)

* .NET 9.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1, 2.0
* .NET Framework 4.8.1 to 4.6.1

Note: All target framework variations are tested only newest it.

----

## How to use

Install [GitReader](https://www.nuget.org/packages/GitReader) from NuGet.

* Install [FSharp.GitReader](https://www.nuget.org/packages/FSharp.GitReader) when you need to use with F#.
  It has F# friendly signature definition.
  You can freely use the same version of a package to switch back and forth between C# and F#,
  as long as the runtime instances are compatible.

GitReader has high-level interfaces and primitive interfaces.

* The high-level interface is an interface that abstracts the Git repository.
  Easy to handle without knowing the internal structure of Git.
  It is possible to retrieve branch, tag, and commit information, and to read files (Blobs) at a glance.
* The primitive interface is an interface that exposes the internal structure of the Git repository as it is,
  It is simple to handle if you know the internal structure of Git,
  and it offers high performance in asynchronous processing.

### Sample Code

Comprehensive sample code can be found in the [samples directory](/samples).
The following things are minimal code fragments.

----

## Samples (High-level interfaces)

The high-level interface is easily referenced by automatically reading much of the information tied to a commit.

### Get current head commit

```csharp
using GitReader;
using GitReader.Structures;

using StructuredRepository repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

// Found current head
if (repository.Head is Branch head)
{
    Console.WriteLine($"Name: {head.Name}");

    // Get the commit that this HEAD points to:
    Commit commit = await head.GetHeadCommitAsync();

    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
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
Console.WriteLine($"IsRemote: {branch.IsRemote}");

Commit commit = await branch.GetHeadCommitAsync();

Console.WriteLine($"Hash: {commit.Hash}");
Console.WriteLine($"Author: {commit.Author}");
Console.WriteLine($"Committer: {commit.Committer}");
Console.WriteLine($"Subject: {commit.Subject}");
Console.WriteLine($"Body: {commit.Body}");
```

Use `BranchesAll` instead of `Branches` when there may be branches with the same name but different references.
Using `Branches` will give you only the first branch available from all branches of the same name.

### Get a tag

```csharp
Tag tag = repository.Tags["1.2.3"];

Console.WriteLine($"Name: {tag.Name}");
Console.WriteLine($"Type: {tag.Type}");
Console.WriteLine($"ObjectHash: {tag.ObjectHash}");

// If present the annotation?
if (tag.HasAnnotation)
{
    // Get tag annotation.
    Annotation annotation = await tag.GetAnnotationAsync();

    Console.WriteLine($"Tagger: {annotation.Tagger}");
    Console.WriteLine($"Message: {annotation.Message}");
}

// If tag is a commit tag?
if (tag.Type == ObjectTypes.Commit)
{
    // Get the commit indicated by the tag.
    Commit commit = await tag.GetCommitAsync();

    // ...
}
```

### Get related branches and tags from a commit

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    // The ReadOnlyArray<T> class is used to protect the inner array.
    // Usage is the same as for general collections such as List<T>.
    ReadOnlyArray<Branch> branches = commit.Branches;
    ReadOnlyArray<Tag> tags = commit.Tags;

    // ...
}
```

### Enumerate branches

```csharp
foreach (Branch branch in repository.Branches.Values)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"IsRemote: {branch.IsRemote}");

    Commit commit = await branch.GetHeadCommitAsync();

    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
}
```

### Enumerate tags

```csharp
foreach (Tag tag in repository.Tags.Values)
{
    Console.WriteLine($"Name: {tag.Name}");
    Console.WriteLine($"Type: {tag.Type}");
    Console.WriteLine($"ObjectHash: {tag.ObjectHash}");
}
```

### Enumerate stashes

```csharp
foreach (Stash stash in repository.Stashes)
{
    Console.WriteLine($"Commit: {stash.Commit.Hash}");
    Console.WriteLine($"Committer: {stash.Committer}");
    Console.WriteLine($"Message: {stash.Message}");
}
```

### Get parent commits

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
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

### Get commit tree information

Tree information is the tree structure of directories and files that are placed when a commit is checked out.
The code shown here does not actually 'check out', but reads these structures as information.

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
{
    TreeRoot treeRoot = await commit.GetTreeRootAsync();

    foreach (TreeEntry entry in treeRoot.Children)
    {
        Console.WriteLine($"Hash: {entry.Hash}");
        Console.WriteLine($"Name: {entry.Name}");
        Console.WriteLine($"Modes: {entry.Modes}");
    }
}
```

### Read blob by stream

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
{
    TreeRoot treeRoot = await commit.GetTreeRootAsync();

    foreach (TreeEntry entry in treeRoot.Children)
    {
        // For Blob, the instance type is `TreeBlobEntry`.
        if (entry is TreeBlobEntry blob)
        {
            using Stream stream = await blob.OpenBlobAsync();

            // (You can access the blob...)
        }
    }
}
```

### Reading a submodule in commit tree.

You can also identify references to submodules.
However, if you want to reference information in a submodule, you must open a new repository.
This is accomplished with `OpenSubModuleAsync()`.

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
{
    TreeRoot treeRoot = await commit.GetTreeRootAsync();

    foreach (TreeEntry entry in treeRoot.Children)
    {
        // For a submodule, the instance type is `TreeSubModuleEntry`.
        if (entry is TreeSubModuleEntry subModule)
        {
            // Open this submodule repository.
            using var subModuleRepository = await subModule.OpenSubModuleAsync();

            // Retreive the commit hash specified in the original repository.
            if (await subModuleRepository.GetCommitAsync(
                subModule.Hash) is Commit subModuleCommit)
            {
                // ...
            }
        }
    }
}
```

### Traverse a branch through primary commits

A commit in Git can have multiple parent commits.
This occurs with merge commits, where there are links to all parent commits.
The first parent commit is called the "primary commit"
and is always present except for the first commit in the repository.

Use `GetPrimaryParentCommitAsync()` to retrieve the primary commit,
and use `GetParentCommitsAsync()` to get links to all parent commits.

As a general thing about Git,
it is important to note that the parent-child relationship of commits (caused by branching and merging),
always expressed as one direction, from "child" to "parent".

This is also true for the high-level interface; there is no interface for referencing a child from its parent.
Therefore, if you wish to perform such a search, you must construct the link in the reverse direction on your own.

The following example recursively searches for a parent commit from a child commit.

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");
Console.WriteLine($"IsRemote: {branch.IsRemote}");

Commit? current = await branch.GetHeadCommitAsync();

// Continue as long as the parent commit exists.
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

### Get worktrees

```csharp
// Get all worktrees for the repository managed
ReadOnlyArray<Worktree> worktrees = await repository.GetWorktreesAsync();

foreach (Worktree worktree in worktrees)
{
    Console.WriteLine($"Name: {worktree.Name}");
    Console.WriteLine($"Path: {worktree.Path}");
    Console.WriteLine($"IsMain: {worktree.IsMain}");
    Console.WriteLine($"Status: {worktree.Status}");
    Console.WriteLine($"Branch: {worktree.Branch ?? "(detached)"}");
    Console.WriteLine($"Head: {worktree.Head?.ToString() ?? "(none)"}");
}
```

### Get working directory status

While other features of GitReader do not read the state of the working directory, but this feature integrates the working directory with the metadata in the repository. Thus, you can get a state that reflects the contents of the ".gitignore" file as well.

```csharp
// Get the current working directory status (“.gitignore” is also reflected.)
WorkingDirectoryStatus status =
    await repository.GetWorkingDirectoryStatusAsync();

// Get staged files
foreach (var entry in status.StagedFiles)
{
    Console.WriteLine($"Staged: {entry.Path}");
}

// Get unstaged files
foreach (var entry in status.UnstagedFiles)
{
    Console.WriteLine($"Unstaged: {entry.Path}");
}

// Get untracked files
foreach (var entry in status.UntrackedFiles)
{
    Console.WriteLine($"Untracked: {entry.Path}");
}

// Get working directory status with custom glob override filter
var globFilter = Glob.CreateExcludeFilter("*.log", "*.tmp", "bin/", "obj/");
WorkingDirectoryStatus filteredStatus =
    await repository.GetWorkingDirectoryStatusWithFilterAsync(globFilter);

// Display filtered files
foreach (var entry in filteredStatus.UntrackedFiles)
{
    Console.WriteLine($"Filtered Untracked: {entry.Path}");
}
```

The glob override filter is applied after evaluation of filters such as ".gitignore".
Thus, rules defined in ".gitignore" can be ignored with a negation condition expression.

----

## Samples (Primitive interfaces)

The high-level interface is implemented internally using these primitive interfaces.
We do not have a complete list of all examples, so we recommend referring to the GitReader code if you need information.

* You may want to start with [StructuredRepositoryFacade class](/GitReader.Core/Structures/StructuredRepositoryFacade.cs).

### Read current head commit

```csharp
using GitReader;
using GitReader.Primitive;

using PrimitiveRepository repository =
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

### Enumerate remote branches

```csharp
PrimitiveReference[] branches = await repository.GetRemoteBranchHeadReferencesAsync();

foreach (PrimitiveReference branch in branches)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"Commit: {branch.Commit}");
}
```

### Enumerate tags

```csharp
PrimitiveTagReference[] tagReferences = await repository.GetTagReferencesAsync();

foreach (PrimitiveTagReference tagReference in tagReferences)
{
    PrimitiveTag tag = await repository.GetTagAsync(tagReference);

    Console.WriteLine($"Hash: {tag.Hash}");
    Console.WriteLine($"Type: {tag.Type}");
    Console.WriteLine($"Name: {tag.Name}");
    Console.WriteLine($"Tagger: {tag.Tagger}");
    Console.WriteLine($"Message: {tag.Message}");
}
```

### Read commit tree information

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
{
    PrimitiveTree tree = await repository.GetTreeAsync(commit.TreeRoot);

    foreach (Hash childHash in tree.Children)
    {
        PrimitiveTreeEntry child = await repository.GetTreeAsync(childHash);

        Console.WriteLine($"Hash: {child.Hash}");
        Console.WriteLine($"Name: {child.Name}");
        Console.WriteLine($"Modes: {child.Modes}");
    }
}
```

### Read blob by stream

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
{
    PrimitiveTree tree = await repository.GetTreeAsync(commit.TreeRoot);

    foreach (Hash childHash in tree.Children)
    {
        PrimitiveTreeEntry child = await repository.GetTreeAsync(childHash);
        if (child.Modes.HasFlag(PrimitiveModeFlags.File))
        {
            using Stream stream = await repository.OpenBlobAsync(child.Hash);

            // (You can access the blob...)
        }
    }
}
```

### Reading a submodule in commit tree.

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
{
    PrimitiveTree tree = await repository.GetTreeAsync(commit.TreeRoot);

    foreach (Hash childHash in tree.Children)
    {
        PrimitiveTreeEntry child = await repository.GetTreeAsync(childHash);

        // If this tree entry is a submodule.
        if (child.SpecialModes == PrimitiveSpecialModes.SubModule)
        {
            // The argument must be a "tree path".
            // It is the sequence of all paths from the repository root up to this entry.
            using var subModuleRepository = await repository.OpenSubModuleAsync(
                new[] { child });

            if (await repository.GetCommitAsync(
                child.Hash) is PrimitiveCommit subModuleCommit)
            {
                // ...
            }
        }
    }
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

## Samples (Others)

### SHA1 hash operations

```csharp
Hash hashFromString = "1205dc34ce48bda28fc543daaf9525a9bb6e6d10";
Hash hashFromArray = new byte[] { 0x12, 0x05, 0xdc, ... };

var hashFromStringConstructor =
    new Hash("1205dc34ce48bda28fc543daaf9525a9bb6e6d10");
var hashFromArrayConstructor =
    new Hash(new byte[] { 0x12, 0x05, 0xdc, ... });

if (Hash.TryParse("1205dc34ce48bda28fc543daaf9525a9bb6e6d10", out Hash hash))
{
    // ...
}

Commit commit = ...;
Hash targetHash = commit;
```

### Signature operations

```csharp
// Create a signature with name, email and date
var sig = new Signature("John Doe", "john@example.com", DateTimeOffset.Now);

// Create a signature with name and date only (no email)
var sigWithoutEmail = new Signature("Jane Doe", DateTimeOffset.Now);

// Parse a signature from Git's raw format
var rawSignature = "John Doe <john@example.com> 1234567890 +0900";
var parsedSig = Signature.Parse(rawSignature);

// Try parse with error handling
if (Signature.TryParse(rawSignature, out Signature sig2))
{
    Console.WriteLine($"Name: {sig2.Name}");
    Console.WriteLine($"Email: {sig2.MailAddress}");
    Console.WriteLine($"Date: {sig2.Date}");
}

// "John Doe <john@example.com>"
Console.WriteLine($"Git author format: {sig.ToGitAuthorString()}");
// "John Doe <john@example.com> 1234567890 +0900"
Console.WriteLine($"Git raw format: {sig.RawFormat}");

// Deconstruct signature
var (name, email, date) = sig;
```

### Enumerate remote urls

```csharp
foreach (KeyValuePair<string, string> entry in repository.RemoteUrls)
{
    Console.WriteLine($"Remote: Name={entry.Key}, Url={entry.Value}");
}
```

### Glob class

The `Glob` class provides a function to parse glob patterns for “.gitignore”.
This implementation is roughly equivalent to glibc's `fnmatch()` function.

```csharp
// Check if path matches pattern
var path = "src/Program.cs";
bool matches = Glob.IsMatch(path, "*.cs");
Console.WriteLine($"Matches: {matches}"); // True

// Create filter with multiple exclude patterns
GlobFilter filter = Glob.CreateExcludeFilter("*.log", "*.tmp", "bin/", "obj/");

// Get common ignore patterns
GlobFilter commonFilter = Glob.GetCommonIgnoreFilter();

// Combine multiple filters. Note that they are applied in order from the first argument.
GlobFilter combinedFilter = Glob.Combine(filter, commonFilter);

// Apply filter to check files
GlobFilterStates filterResult = Glob.ApplyFilter(combinedFilter, "debug.log");
Console.WriteLine($"Filter result: {filterResult}"); // Exclude

// Filter that excludes all files
GlobFilter excludeAllFilter = Glob.GetExcludeAllFilter();
```

The following method can be used to read ".gitignore" and generate a glob filter:

```csharp 
// Create filter from .gitignore file 
using var stream = File.OpenRead(".gitignore"); 
GlobFilter filter1 = await Glob.CreateExcludeFilterFromGitignoreAsync(stream, ct);

// Create .gitignore filter from TextReader 
using var reader = File.OpenText(".gitignore"); 
GlobFilter filter2 = await Glob.CreateExcludeFilterFromGitignoreAsync(reader, ct);

// Create .gitignore filter from an array of strings 
var gitignoreLines = new[] { "*.log", "bin/", "obj/", "# comment", "" }; 
GlobFilter filter3 = Glob.CreateExcludeFilterFromGitignore(gitignoreLines); 
```

`GetCommonIgnoreFilter()`で得られるルールは、以下の通りです:

|Type|Pattern|
|:----|:----|
|Build outputs|"bin/", "obj/", "build/", "out/", "target/", "dist/"|
|Dependencies|"node_modules/", "packages/", "vendor/"|
|Log files|"*.log", "logs/"|
|Temporary files|"*.tmp", "*.temp", "*.swp", "*.bak", "*~"|
|IDE files|".vs/", ".vscode/", ".idea/", "*.suo", "*.user"|
|OS files|".DS_Store", "Thumbs.db", "Desktop.ini"|
|Version control files|"*.orig", "*.rej"|

This rule may not be useful since official Git does not define any standard patterns (except for ".git" which is forcibly excluded).

Note: The `Glob` class assumes the path format to be POSIX path format. This means that the “backslash” character is part of the directory name or filename. When GitReader searches for a working directory, it replaces the backslash character with a slash character, depending on whether you are in a Windows environment or not. This is done by the `StandardFileSystem.ToPosixPath()` method.

----

## Contributed (Thanks!)

* Stash implementation: [Julien Richard](https://github.com/jairbubbles)

## License

Apache-v2

## History

* 1.13.0:
  * Improved performance for working directory traverser.
* 1.12.0:
  * Supported ".gitignore" interpretation. It is used to traverse working directory. (#16)
  * Added globbing parser/interpreter, for use ".gitignore". (#16)
  * Improved performance for working directory traverser. (#16)
  * Fixed raise MAE on access F# library on Release building.
    * This could occur when functions are automatically inlined at F# compile time.
  * We do not think it is a problem since it was a short period of time, but please note that there is a disruptive change in the working directory related API that was added in 1.11.0.
* 1.11.0:
  * Added worktree accessor.
  * Added index (working directory) information accessor.
  * Added XML comments.
* 1.10.0:
  * Added Git worktree detection. (#15)
* 1.9.0:
  * Supported multiple same named branches.
  * Added .NET 9.0 tfm.
  * Fixed IOR exception when triggered loading large offset table. (#14)
* 1.8.0:
  * Added file system abstraction interface called `IFileSystem`.
    * This interface allows repository access independent of the local file system.
    * Currently undocumented, but there is a `StandardFileSystem` class that uses `System.IO` as its default implementation, so you may want to refer to this class.
* 1.7.0:
  * Rebuilt on .NET 8.0 SDK.
* 1.6.0:
  * Added submodule accessor.
  * Fixed invalid remote url entries at multiple declaration. (#10)
* 1.5.0:
  * Included .NET 8.0 RC2 assembly (`net8.0`).
* 1.4.0:
  * Improved stability to open metadata files, avoids file sharing violation.
* 1.3.0:
  * Fixed internal cached streams locked when disposed the repository. (#9)
* 1.2.0:
  * Added `Repository.getCurrentHead()` function on F# interface.
  * Uses ArrayPool on both netcoreapp and netstandard2.1.
* 1.1.0:
  * Fixed causing path not found exception when lack these directories.
  * Added debugger display attribute on some types.
* 1.0.0:
  * Reached 1.0.0 :tada:
  * Fixed broken decoded stream for deltified stream with derived large-base-stream.
* 0.13.0:
  * Improved performance.
* 0.12.0:
  * Reduced the time taken to open structured repository when peeled-tag is available from packed-refs.
  * The Tags interface has been rearranged.
  * Added raw stream opener interfaces.
  * Some bug fixed.
* 0.11.0:
  * The structured interface no longer reads commit information when it opens.
    Instead, you must explicitly call `Branch.GetHeadCommitAsync()`,
    but the open will be processed much faster.  
  * Improved performance.
* 0.10.0:
  * Implemented stash interface.
  * Improved performance.
  * Fixed minor corruption on blob data when it arrives zero-sized deltified data.
* 0.9.0:
  * Exposed remote urls.
  * Changed some type names avoid confliction.
* 0.8.0:
  * Added tree/blob accessors.
  * Improved performance.
* 0.7.0:
  * Switched primitive interface types with prefix `Primitive`.
  * Improved performance.
  * Tested large repositories.
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
