# GitReader

軽量なGitローカルリポジトリ参照ライブラリ

![GitReader](Images/GitReader.100.png)

# Status

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

|Target|Pakcage|
|:----|:----|
|Any|[![NuGet GitReader](https://img.shields.io/nuget/v/GitReader.svg?style=flat)](https://www.nuget.org/packages/GitReader)|
|F# binding|[![NuGet FSharp.GitReader](https://img.shields.io/nuget/v/FSharp.GitReader.svg?style=flat)](https://www.nuget.org/packages/FSharp.GitReader)|

----

[English language is here](https://github.com/kekyo/GitReader)

## これは何?

.NETでGitのローカルリポジトリの情報にアクセスしたいと考えたことはありますか？
ブランチの探索やタグ、コミットの日付や貢献者の情報の取得、そしてコミットのディレクトリ構造やファイルの読み取りなど。

GitReaderは、幅広い.NET環境に対応し、マネージドコードだけで書かれた、Gitローカルリポジトリ参照ライブラリです。
軽量で、簡潔で使いやすいインターフェイスを持ち、他のライブラリに依存せず、ネイティブのライブラリも含んでいません。
どんな環境にも対応できるようにしました。

コードの例:

```csharp
using GitReader;
using GitReader.Structures;

using var repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

if (repository.GetCurrentHead() is { } head)
{
    Console.WriteLine($"Name: {head.Name}");

    Console.WriteLine($"Hash: {head.Head.Hash}");
    Console.WriteLine($"Author: {head.Head.Author}");
    Console.WriteLine($"Committer: {head.Head.Committer}");
    Console.WriteLine($"Subject: {head.Head.Subject}");
    Console.WriteLine($"Body: {head.Head.Body}");
}
```

以下のような特徴があります:

* Gitのブランチ、タグ、コミットに関する情報を取得できます。
* ブランチツリーの探索が可能です。
* 読み取り専用インターフェイスで、イミュータビリティを実現。
* 高レベルとプリミティブの、両方のインターフェイスが用意されています。
* 完全な非同期処理と非同期インターフェイス。
* 100%マネージドコードのみ。BCLとその準拠ライブラリ以外の外部ライブラリに依存しない。
* .NET標準のdeflate実装を使用した、信頼性の高いzlib解凍。

このライブラリは、[RelaxVersioner](https://github.com/kekyo/CenterCLR.RelaxVersioner) が依存していた
`libgit2sharp` を置き換えるために、フルスクラッチで設計されました。
主に、Gitリポジトリからコミット情報を簡単に抽出する目的に適しています。

### 対応する.NETプラットフォーム

* .NET 7.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1 to 1.6
* .NET Framework 4.8.1 to 3.5

### F#専用のバインディング

F# 5.0以上が対象で、F#フレンドリーなシグネチャ定義が含まれています。

* .NET 7.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1, 2.0
* .NET Framework 4.8.1 to 4.6.1

注意: 全てのターゲットフレームワークのバリエーションは、最新のもののみテストされています。

----

## 使い方

NuGetの [GitReader](https://www.nuget.org/packages/GitReader) をインストールします。

* F#を使っているなら、 [FSharp.GitReader](https://www.nuget.org/packages/FSharp.GitReader) を使うほうが良いでしょう。
  このパッケージは、F#フレンドリーな定義を公開しています。
  同じバージョンのパッケージであれば、実行時のインスタンスに互換性があるため、C#とF#を行き来する場合でも自由に使うことが出来ます。

GitReaderには、高レベルインターフェースとプリミティブインターフェースがあります。

* 高レベルインターフェースは、Gitリポジトリを抽象化したインターフェースです。
  Gitの内部構造を知らなくても容易に扱えます。ブランチ、タグ、コミット情報の取得や、ファイル(Blob)の読み取りが簡単です。
* プリミティブインターフェースは、Gitリポジトリの内部構造を、ほぼそのまま公開するインターフェースです。
  Gitの内部構造を知っていれば扱いやすく、非同期処理で高性能を発揮します。
  
### サンプルコード

総合的なサンプルコードは、 [サンプルディレクトリ](/samples) に存在します。
以下では、最小のコード断片を示します。

----

## サンプルコード (高レベルインターフェイス)

高レベルインターフェイスは、コミットに紐づく多くの情報が自動的に読み取られることで、簡単に参照する事が出来ます。

### 現在のHEADの情報を取得

```csharp
using GitReader;
using GitReader.Structures;

using StructuredRepository repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

// 現在のHEADが見つかった
if (repository.GetCurrentHead() is Branch head)
{
    Console.WriteLine($"Name: {head.Name}");

    Console.WriteLine($"Hash: {head.Head.Hash}");
    Console.WriteLine($"Author: {head.Head.Author}");
    Console.WriteLine($"Committer: {head.Head.Committer}");
    Console.WriteLine($"Subject: {head.Head.Subject}");
    Console.WriteLine($"Body: {head.Head.Body}");
}
```

### 指定されたコミットの情報を直接取得

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

### 指定されたブランチの情報を取得

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Subject: {branch.Head.Subject}");
Console.WriteLine($"Body: {branch.Head.Body}");
```

### 指定されたリモートブランチの情報を取得

```csharp
Branch branch = repository.RemoteBranches["origin/develop"];

Console.WriteLine($"Name: {branch.Name}");

Console.WriteLine($"Hash: {branch.Head.Hash}");
Console.WriteLine($"Author: {branch.Head.Author}");
Console.WriteLine($"Committer: {branch.Head.Committer}");
Console.WriteLine($"Subject: {branch.Head.Subject}");
Console.WriteLine($"Body: {branch.Head.Body}");
```

### タグの情報を取得

```csharp
Tag tag = repository.Tags["1.2.3"];

Console.WriteLine($"Name: {tag.Name}");

Console.WriteLine($"Hash: {tag.Hash}");
Console.WriteLine($"Author: {tag.Author}");
Console.WriteLine($"Committer: {tag.Committer}");
Console.WriteLine($"Message: {tag.Message}");
```

### 指定されたコミットに紐づくブランチやタグの情報を取得

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    // ReadOnlyArray<T>クラスは、内部の配列を保護するために使用されます。
    // 使用方法はList<T>のような一般的なコレクションと同じです。
    ReadOnlyArray<Branch> branches = commit.Branches;
    ReadOnlyArray<Branch> remoteBranches = commit.RemoteBranches;
    ReadOnlyArray<Tag> tags = commit.Tags;

    // ...
}
```

### このリポジトリのブランチ群の情報を取得

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

### このリポジトリのタグ群の情報を取得

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

### このリポジトリのスタッシュ群の情報を取得

```csharp
foreach (Stash stash in repository.Stashes)
{
    Console.WriteLine($"Commit: {stash.Commit.Hash}");
    Console.WriteLine($"Committer: {stash.Committer}");
    Console.WriteLine($"Message: {stash.Message}");
}
```

### 指定されたコミットの親コミット群を取得

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

### 指定されたコミットのツリー情報を取得

ツリー情報とは、コミットをチェックアウトしたときに配置される、ディレクトリ群とファイル群のツリー構造の事です。
ここで示すコードでは、実際にチェックアウトする訳ではなく、これらの構造を情報として読み取ります。

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Command commit)
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

### コミット内のファイル(Blob)の読み取り

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Command commit)
{
    TreeRoot treeRoot = await commit.GetTreeRootAsync();

    foreach (TreeEntry entry in treeRoot.Children)
    {
        // Blobの場合はインスタンスの型が`TreeBlobEntry`です
        if (entry is TreeBlobEntry blob)
        {
            using var stream = await blob.OpenBlobAsync();

            // (Blobにアクセスすることができます...)
        }
    }
}
```

### コミットのプライマリ親コミットを再帰的に取得

Gitのコミットは、複数の親コミットを持つ事があります。
これはマージコミットで発生し、全ての親コミットへのリンクが存在します。
最初の親コミットの事を「プライマリコミット」と呼び、リポジトリの最初のコミット以外には必ず存在します。

全ての親コミットへのリンクを取得するには、`GetParentCommitsAsync()` を使用します。

注意する点として、コミットの親子関係（ブランチとマージによって発生する）は、
常に「子」から「親」方向への一方向として表現されます。
これは高レベルインターフェイスでも同様で、親から子を参照するためのインターフェイスはありません。
そのため、このような探索を行いたい場合は、自力で逆方向のリンクを構築する必要があります。

以下の例では、子コミットから親コミットを再帰的に探索します。

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");

Commit? current = branch.Head;

// 親コミットが存在する限り続ける
while (current != null)
{
    Console.WriteLine($"Hash: {current.Hash}");
    Console.WriteLine($"Author: {current.Author}");
    Console.WriteLine($"Committer: {current.Committer}");
    Console.WriteLine($"Subject: {current.Subject}");
    Console.WriteLine($"Body: {current.Body}");

    // プライマリ親コミットを取得
    current = await current.GetPrimaryParentCommitAsync();
}
```

----

## サンプルコード (プリミティブインターフェイス)

ハイレベルインターフェイスは、内部でこれらのプリミティブインターフェイスを使用して実装しています。
全ての例を網羅していないため、情報が必要であればGitReaderのコードを参照する事をお勧めします。

* [RepositoryFacadeクラス](/GitReader.Core/Structures/RepositoryFacade.cs) から始めると良いでしょう。

### 現在のHEADの情報を取得

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

### 指定されたコミットの情報を直接取得

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

### 指定されたブランチの情報を取得

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

### このリポジトリのブランチ群の情報を取得

```csharp
PrimitiveReference[] branches = await repository.GetBranchHeadReferencesAsync();

foreach (PrimitiveReference branch in branches)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"Commit: {branch.Commit}");
}
```

### このリポジトリのタグ群の情報を取得

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

### 指定されたコミットのツリー情報を取得

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

### コミット内のファイル(Blob)の読み取り

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
            using var stream = await repository.OpenBlobAsync(child.Hash);

            // (You can access the blob...)
        }
    }
}
```

### コミットのプライマリ親コミットを再帰的に取得

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

        // 現在のブランチは最初のコミット
        if (commit.Parents.Length == 0)
        {
            break;
        }

        // プライマリ親コミットを取得
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

## サンプルコード (その他)

### SHA1ハッシュの操作

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

### リモートリポジトリURLの列挙

```csharp
foreach (KeyValuePair<string, string> entry in repository.RemoteUrls)
{
    Console.WriteLine($"Remote: Name={entry.Key}, Url={entry.Value}");
}
```

----

## TODO

* [英語READMEを参照してください](https://github.com/kekyo/GitReader)

----

## License

Apache-v2

## History

* [英語READMEを参照してください](https://github.com/kekyo/GitReader)
