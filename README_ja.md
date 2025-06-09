# GitReader

軽量なGitローカルリポジトリ参照ライブラリ

![GitReader](Images/GitReader.100.png)

# Status

[![Project Status: Active – The project has reached a stable, usable state and is being actively developed.](https://www.repostatus.org/badges/latest/active.svg)](https://www.repostatus.org/#active)

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

// リポジトリをオープン (高レベルインターフェイスを使用)
using var repository =
    await Repository.Factory.OpenStructureAsync(
        "/home/kekyo/Projects/YourOwnLocalGitRepo");

// HEADが存在すれば
if (repository.Head is { } head)
{
    Console.WriteLine($"Name: {head.Name}");

    // HEADのコミットを得る
    var commit = await head.GetHeadCommitAsync();

    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
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

* .NET 9.0 to 5.0
* .NET Core 3.1 to 2.0
* .NET Standard 2.1 to 1.6
* .NET Framework 4.8.1 to 3.5

### F#専用のバインディング

F# 5.0以上が対象で、F#フレンドリーなシグネチャ定義が含まれています。
(`Async`型による非同期操作、`Option`によるnull値排除など)

* .NET 9.0 to 5.0
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
if (repository.Head is Branch head)
{
    Console.WriteLine($"Name: {head.Name}");

    // このHEADが指すコミットを得る
    Commit commit = await head.GetHeadCommitAsync();

    Console.WriteLine($"Hash: {commit.Hash}");
    Console.WriteLine($"Author: {commit.Author}");
    Console.WriteLine($"Committer: {commit.Committer}");
    Console.WriteLine($"Subject: {commit.Subject}");
    Console.WriteLine($"Body: {commit.Body}");
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
Console.WriteLine($"IsRemote: {branch.IsRemote}");

Commit commit = await branch.GetHeadCommitAsync();

Console.WriteLine($"Hash: {commit.Hash}");
Console.WriteLine($"Author: {commit.Author}");
Console.WriteLine($"Committer: {commit.Committer}");
Console.WriteLine($"Subject: {commit.Subject}");
Console.WriteLine($"Body: {commit.Body}");
```

同じ名前を持つ、異なる参照のブランチが存在する可能性がある場合は、`Branches` の代わりに `BranchesAll` を使います。
`Branches` を使うと、同名の全てのブランチから得られる最初のブランチのみが得られます。

### 指定されたタグの情報を取得

```csharp
Tag tag = repository.Tags["1.2.3"];

Console.WriteLine($"Name: {tag.Name}");
Console.WriteLine($"Type: {tag.Type}");
Console.WriteLine($"ObjectHash: {tag.ObjectHash}");

// アノテーションが存在すれば
if (tag.HasAnnotation)
{
    // タグのアノテーションを取得
    Annotation annotation = await tag.GetAnnotationAsync();

    Console.WriteLine($"Tagger: {annotation.Tagger}");
    Console.WriteLine($"Message: {annotation.Message}");
}

// コミットタグなら
if (tag.Type == ObjectTypes.Commit)
{
    // タグが示すコミットを取得
    Commit commit = await tag.GetCommitAsync();

    // ...
}
```

### 指定されたコミットに紐づくブランチやタグの情報を取得

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is Commit commit)
{
    // ReadOnlyArray<T>クラスは、内部の配列を保護するために使用されます。
    // 使用方法はList<T>のような一般的なコレクションと同じです。
    ReadOnlyArray<Branch> branches = commit.Branches;
    ReadOnlyArray<Tag> tags = commit.Tags;

    // ...
}
```

### このリポジトリのブランチ群の情報を取得

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

### このリポジトリのタグ群の情報を取得

```csharp
foreach (Tag tag in repository.Tags.Values)
{
    Console.WriteLine($"Name: {tag.Name}");
    Console.WriteLine($"Type: {tag.Type}");
    Console.WriteLine($"ObjectHash: {tag.ObjectHash}");
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

### 指定されたコミットのツリー情報を取得

ツリー情報とは、コミットをチェックアウトしたときに配置される、ディレクトリ群とファイル群のツリー構造の事です。
ここで示すコードでは、実際にチェックアウトする訳ではなく、これらの構造を情報として読み取ります。

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

### コミット内のファイル(Blob)の読み取り

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
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

### コミット内のサブモジュールの読み取り

サブモジュールへの参照を識別することも出来ます。
但し、サブモジュール内の情報を参照する場合は、新たにリポジトリをオープンすることになります。
これを実現するのが、 `OpenSubModuleAsync()` です。

```csharp
if (await repository.GetCommitAsync(
    "6961a50ef3ad4e43ed9774daffd8457d32cf5e75") is Commit commit)
{
    TreeRoot treeRoot = await commit.GetTreeRootAsync();

    foreach (TreeEntry entry in treeRoot.Children)
    {
        // サブモジュールの場合はインスタンスの型が`TreeSubModuleEntry`です
        if (entry is TreeSubModuleEntry subModule)
        {
            // サブモジュールリポジトリをオープンします
            using var subModuleRepository = await subModule.OpenSubModuleAsync();

            // 元のリポジトリで指定されているコミットを参照して取得します
            if (await subModuleRepository.GetCommitAsync(
                subModule.Hash) is Commit subModuleCommit)
            {
                // ...
            }
        }
    }
}
```

### コミットのプライマリ親コミットを再帰的に取得

Gitのコミットは、複数の親コミットを持つ事があります。
これはマージコミットで発生し、全ての親コミットへのリンクが存在します。
最初の親コミットの事を「プライマリコミット」と呼び、リポジトリの最初のコミット以外には必ず存在します。

プライマリコミットを取得する場合は、 `GetPrimaryParentCommitAsync()` を、
全ての親コミットへのリンクを取得する場合は、`GetParentCommitsAsync()` を使用します。

注意する点として、コミットの親子関係（ブランチとマージによって発生する）は、
常に「子」から「親」方向への一方向として表現されます。
これは高レベルインターフェイスでも同様で、親から子を参照するためのインターフェイスはありません。
そのため、このような探索を行いたい場合は、自力で逆方向のリンクを構築する必要があります。

以下の例では、子コミットから親コミットを再帰的に探索します。

```csharp
Branch branch = repository.Branches["develop"];

Console.WriteLine($"Name: {branch.Name}");
Console.WriteLine($"IsRemote: {branch.IsRemote}");

Commit? current = await branch.GetHeadCommitAsync();

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

### ワーキングディレクトリの状態を取得

```csharp
// 現在のワーキングディレクトリの状態を取得
WorkingDirectoryStatus status = await repository.GetWorkingDirectoryStatusAsync();

// 変更があるかどうかを確認
if (status.HasChanges)
{
    // 変更されたファイルを取得
    foreach (var entry in status.Modified)
    {
        Console.WriteLine($"Modified: {entry.Path}");
    }

    // 未追跡のファイルを取得
    foreach (var entry in status.Untracked)
    {
        Console.WriteLine($"Untracked: {entry.Path}");
    }

    // 削除されたファイルを取得
    foreach (var entry in status.Deleted)
    {
        Console.WriteLine($"Deleted: {entry.Path}");
    }
}
```

----

## サンプルコード (プリミティブインターフェイス)

ハイレベルインターフェイスは、内部でこれらのプリミティブインターフェイスを使用して実装しています。
全ての例を網羅していないため、情報が必要であればGitReaderのコードを参照する事をお勧めします。

* [StructuredRepositoryFacadeクラス](/GitReader.Core/Structures/StructuredRepositoryFacade.cs) から始めると良いでしょう。

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

### このリポジトリのリモートブランチ群の情報を取得

```csharp
PrimitiveReference[] branches = await repository.GetRemoteBranchHeadReferencesAsync();

foreach (PrimitiveReference branch in branches)
{
    Console.WriteLine($"Name: {branch.Name}");
    Console.WriteLine($"Commit: {branch.Commit}");
}
```

### このリポジトリのタグ群の情報を取得

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

### コミット内のサブモジュールの読み取り

```csharp
if (await repository.GetCommitAsync(
    "1205dc34ce48bda28fc543daaf9525a9bb6e6d10") is PrimitiveCommit commit)
{
    PrimitiveTree tree = await repository.GetTreeAsync(commit.TreeRoot);

    foreach (Hash childHash in tree.Children)
    {
        PrimitiveTreeEntry child = await repository.GetTreeAsync(childHash);

        // このツリーエントリがサブモジュールの場合
        if (child.SpecialModes == PrimitiveSpecialModes.SubModule)
        {
            // 引数には「ツリーパス」が必要です。
            // これは、このエントリに至るまでの、リポジトリルートからの全てのパス列です。
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

## License

Apache-v2

## History

* [英語READMEを参照してください](https://github.com/kekyo/GitReader#history)
