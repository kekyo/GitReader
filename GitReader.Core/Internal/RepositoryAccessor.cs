////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.IO;
using GitReader.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal enum ReferenceTypes
{
    Branches,
    RemoteBranches,
}

internal readonly struct TagReference
{
    public readonly Hash ObjectOrCommitHash;
    public readonly Hash? CommitHash;

    public TagReference(Hash objectOrCommitHash, Hash? commitHash)
    {
        this.ObjectOrCommitHash = objectOrCommitHash;
        this.CommitHash = commitHash;
    }
}

internal readonly struct ReferenceCache
{
    public readonly ReadOnlyDictionary<string, Hash> Branches;
    public readonly ReadOnlyDictionary<string, Hash> RemoteBranches;
    public readonly ReadOnlyDictionary<string, TagReference> Tags;

    public ReferenceCache(
        ReadOnlyDictionary<string, Hash> branches,
        ReadOnlyDictionary<string, Hash> remoteBranches,
        ReadOnlyDictionary<string, TagReference> tags)
    {
        this.Branches = branches;
        this.RemoteBranches = remoteBranches;
        this.Tags = tags;
    }

    public ReferenceCache Combine(ReferenceCache rhs)
    {
        var branches = this.Branches.Clone();
        var remoteBranches = this.RemoteBranches.Clone();
        var tags = this.Tags.Clone();
        
        foreach (var entry in rhs.Branches)
        {
            branches[entry.Key] = entry.Value;
        }
        foreach (var entry in rhs.RemoteBranches)
        {
            remoteBranches[entry.Key] = entry.Value;
        }
        foreach (var entry in rhs.Tags)
        {
            tags[entry.Key] = entry.Value;
        }

        return new(branches, remoteBranches, tags);
    }
}

internal readonly struct HashResults
{
    public readonly Hash Hash;
    public readonly string[] Names;

    public HashResults(Hash hash, string[] names)
    {
        this.Hash = hash;
        this.Names = names;
    }
}

internal static class RepositoryAccessor
{
    public readonly record struct CandidateRepositoryPaths(
        string GitPath,
        string[] AlternativePaths);
    
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<CandidateRepositoryPaths> DetectLocalRepositoryPathAsync(
        string startPath, IFileSystem fileSystem, CancellationToken ct)
#else
    public static async Task<CandidateRepositoryPaths> DetectLocalRepositoryPathAsync(
        string startPath, IFileSystem fileSystem, CancellationToken ct)
#endif
    {
        var currentPath = fileSystem.GetFullPath(startPath);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var candidatePath = Path.GetFileName(currentPath) != ".git" ?
                fileSystem.Combine(currentPath, ".git") : currentPath;

            if (await fileSystem.IsFileExistsAsync(fileSystem.Combine(candidatePath, "config"), ct))
            {
                return new(candidatePath, Utilities.Empty<string>());
            }

            // Issue #11
            if (await fileSystem.IsFileExistsAsync(candidatePath, ct))
            {
                using var fs = await fileSystem.OpenAsync(candidatePath, false, ct);
                var tr = new AsyncTextReader(fs);

                while (true)
                {
                    var line = await tr.ReadLineAsync(ct);
                    if (line == null)
                    {
                        break;
                    }

                    line = line.Trim();
                    if (line.StartsWith("gitdir:"))
                    {
                        // Resolve to full path (And normalize path directory separators)
                        var gitDirPath = line.Substring(7).TrimStart();
                        candidatePath = fileSystem.ResolveRelativePath(currentPath, gitDirPath);

                        if (await fileSystem.IsFileExistsAsync(fileSystem.Combine(candidatePath, "config"), ct))
                        {
                            return new(candidatePath, Utilities.Empty<string>());
                        }

                        // Worktree
                        var commonDirPath = fileSystem.Combine(candidatePath, "commondir");
                        if (await fileSystem.IsFileExistsAsync(commonDirPath, ct))
                        {
                            using var fs2 = await fileSystem.OpenAsync(commonDirPath, false, ct);
                            var tr2 = new AsyncTextReader(fs2);

                            var relativePath = (await tr2.ReadToEndAsync(ct)).Trim();
                            var gitPath = fileSystem.GetFullPath(fileSystem.Combine(candidatePath, relativePath));
                            return new(gitPath, [candidatePath]);
                        }

                        break;
                    }
                }

                throw new ArgumentException(
                    "Repository does not exist. `.git` file exists. But failed to `gitdir` or specified directory is not exists.");
            }

            if (Path.GetPathRoot(currentPath) == currentPath)
            {
                throw new ArgumentException("Repository does not exist.");
            }

            currentPath = fileSystem.GetDirectoryPath(currentPath);
        }
    }

    internal readonly struct CandidateFilePath
    {
        public readonly string GitPath;
        public readonly string BasePath;
        public readonly string Path;
        
        public CandidateFilePath(string gitPath, string basePath, string path)
        {
            this.GitPath = gitPath;
            this.BasePath = basePath;
            this.Path = path;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<CandidateFilePath?> GetCandidateFilePathAsync(
#else
    public static async Task<CandidateFilePath?> GetCandidateFilePathAsync(
#endif
        Repository repository,
        string relativePathFromGitPath,
        CancellationToken ct)
    {
        foreach (var gitPath in repository.TryingPathList)
        {
            var candidatePath = repository.fileSystem.Combine(gitPath, relativePathFromGitPath);
            if (await repository.fileSystem.IsFileExistsAsync(candidatePath, ct))
            {
                return new(gitPath, repository.fileSystem.GetDirectoryPath(candidatePath), candidatePath);
            }
        }
        return null;
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    public static async ValueTask<CandidateFilePath[]> GetCandidateFilePathsAsync(
#else
    public static async Task<CandidateFilePath[]> GetCandidateFilePathsAsync(
#endif
        Repository repository,
        string relativePathFromGitPath,
        string match,
        CancellationToken ct) =>
        (await Utilities.WhenAll(repository.TryingPathList.
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
            Select((Func<string, ValueTask<CandidateFilePath[]>>)(async gitPath =>
#else
            Select((async gitPath =>
#endif
        {
            var basePath = repository.fileSystem.Combine(gitPath, relativePathFromGitPath);
            var candidatePaths = await repository.fileSystem.GetFilesAsync(basePath, match, ct);
            return candidatePaths.
                Select(candidatePath => new CandidateFilePath(gitPath, basePath, candidatePath)).
                ToArray();
        })))).
        SelectMany(paths => paths).
        ToArray();

    public static string GetReferenceTypeName(ReferenceTypes type) =>
        type switch
        {
            ReferenceTypes.Branches => "heads",
            ReferenceTypes.RemoteBranches => "remotes",
            _ => throw new ArgumentException(),
        };

    private static bool TryExtractRemoteName(string line, out string remoteName)
    {
        if (line.StartsWith("[") && line.EndsWith("]"))
        {
            var sectionName = line.Substring(1, line.Length - 2).Trim();
            if (sectionName.Length >= 1)
            {
                if (sectionName.StartsWith("remote "))
                {
                    var startIndex = sectionName.IndexOf('"', 7);
                    if (startIndex >= 0)
                    {
                        var endIndex = sectionName.IndexOf('"', startIndex + 1);
                        if (endIndex >= 0)
                        {
                            var name = sectionName.Substring(
                                startIndex + 1, endIndex - startIndex - 1);
                            if (name.Length >= 1)
                            {
                                remoteName = name;
                                return true;
                            }
                        }
                    }
                }
            }
        }
        remoteName = null!;
        return false;
    }

    public static async Task<ReadOnlyDictionary<string, string>> ReadRemoteReferencesAsync(
        Repository repository,
        CancellationToken ct)
    {
        if (await GetCandidateFilePathAsync(repository, "config", ct) is not { } cp)
        {
            return new(new());
        }

        using var fs = await repository.fileSystem.OpenAsync(cp.Path, false, ct);
        var tr = new AsyncTextReader(fs);

        var remotes = new Dictionary<string, string>();
        var remoteName = default(string);

        while (true)
        {
            var line = await tr.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            line = line.Trim();

            if (TryExtractRemoteName(line, out var rn))
            {
                remoteName = rn;
            }
            else if (remoteName != null && line.StartsWith("url"))
            {
                if (line.Split('=').ElementAtOrDefault(1)?.Trim() is { } urlString)
                {
                    remotes[remoteName] = urlString;
                    remoteName = null;
                }
            }
        }

        return new(remotes);
    }

    public static async Task<ReferenceCache> ReadFetchHeadsAsync(
       Repository repository,
       CancellationToken ct)
    {
        Debug.Assert(repository.remoteUrls != null);

        var remoteNameByUrl = repository.remoteUrls.
            ToDictionary(entry => entry.Value, entry => entry.Key);

        if (await GetCandidateFilePathAsync(repository, "FETCH_HEAD", ct) is not { } cp)
        {
            return new(new(new()), new(new()), new(new()));
        }

        using var fs = await repository.fileSystem.OpenAsync(cp.Path, false, ct);
        var tr = new AsyncTextReader(fs);

        var remoteBranches = new Dictionary<string, Hash>();
        var tags = new Dictionary<string, TagReference>();

        while (true)
        {
            var line = await tr.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            var columns = line.Split('\t');
            if (columns.Length < 3)
            {
                continue;
            }

            var hashCodeString = columns[0].Trim();
            var descriptorString = columns[2].Trim();

            if (!Hash.TryParse(hashCodeString, out var hash))
            {
                continue;
            }

            var column0Separator = descriptorString.IndexOfAny(new[] { ' ', '\t' });
            if (column0Separator < 0)
            {
                continue;
            }

            var typeString = descriptorString.Substring(0, column0Separator);
            if (typeString != "branch" && typeString != "tag")
            {
                continue;
            }

            // FETCH_HEAD file is a log file, so will be overwrite dictionary entry.

            descriptorString = descriptorString.Substring(column0Separator + 1);
            if (descriptorString.StartsWith("\'"))
            {
                var qi = descriptorString.IndexOf('\'', 1);
                if (qi < 0)
                {
                    continue;
                }

                var name = descriptorString.Substring(1, qi - 1);

                if (typeString == "branch")
                {
                    var urlString = descriptorString.Split(' ').Last();
                    if (remoteNameByUrl.TryGetValue(urlString, out var remoteName))
                    {
                        remoteBranches[$"{remoteName}/{name}"] = hash;
                    }
                }
                else
                {
                    tags[name] = new(hash, null);
                }
            }
            else
            {
                if (typeString == "branch")
                {
                    remoteBranches[descriptorString] = hash;
                }
                else
                {
                    tags[descriptorString] = new(hash, null);
                }
            }
        }

        return new(new(new()), remoteBranches, tags);
    }

    public static async Task<ReferenceCache> ReadPackedRefsAsync(
        Repository repository,
        CancellationToken ct)
    {
        if (await GetCandidateFilePathAsync(repository, "packed-refs", ct) is not { } cp)
        {
            return new(new(new()), new(new()), new(new()));
        }

        using var fs = await repository.fileSystem.OpenAsync(cp.Path, false, ct);
        var tr = new AsyncTextReader(fs);

        var branches = new Dictionary<string, Hash>();
        var remoteBranches = new Dictionary<string, Hash>();
        var tags = new Dictionary<string, TagReference>();

        var separators = new[] { ' ' };

        string? tagName = null;
        Hash? tagHash = null;

        while (true)
        {
            var line = await tr.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            var columns = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length >= 3)
            {
                continue;
            }

            var hashCodeString = columns[0];

            // Scheduled tag.
            if (tagName is { } tn && tagHash is { } th)
            {
                tagName = null;
                tagHash = null;

                // Detected peeled-tag.
                if (columns.Length == 1 &&
                    hashCodeString.StartsWith("^") &&
                    Hash.TryParse(hashCodeString.Substring(1), out var commitHash))
                {
                    // Provided commit hash.
                    tags![tn] = new(th, commitHash);
                    continue;
                }
                else
                {
                    // Unspecified tag.
                    // This could be a tag object hash or a commit object hash.
                    // We cannot use this hash to determine without reading the object.
                    // See `StructuredRepositoryFacade.GetStructuredTagsAsync()`.
                    tags![tn] = new(th, null);
                }
            }

            if (columns.Length != 2 ||
                !Hash.TryParse(hashCodeString, out var hash))
            {
                continue;
            }

            var referenceString = columns[1];
            if (referenceString.StartsWith("refs/remotes/"))
            {
                var name = referenceString.Substring("refs/remotes/".Length);
                remoteBranches[name] = hash;
            }
            else if (referenceString.StartsWith("refs/heads/"))
            {
                var name = referenceString.Substring("refs/heads/".Length);
                branches[name] = hash;
            }
            else if (referenceString.StartsWith("refs/tags/"))
            {
                // Scheduled unspecified tag.
                tagName = referenceString.Substring("refs/tags/".Length);
                tagHash = hash;
            }
        }

        // Scheduled tag.
        if (tagName is { } tn2 && tagHash is { } th2)
        {
            // Unspecified tag.
            tags![tn2] = new(th2, null);
        }

        return new(branches, remoteBranches, tags);
    }

    //////////////////////////////////////////////////////////////////////////

    public static async Task<HashResults?> ReadHashAsync(
        Repository repository,
        string relativePathOrLocation,
        CancellationToken ct)
    {
        var currentLocation = relativePathOrLocation.
            Replace(Path.DirectorySeparatorChar, '/');
        var names = new List<string>();

        while (true)
        {
            var name = currentLocation.
                Replace("refs/heads/", string.Empty).
                Replace("refs/remotes/", string.Empty).
                Replace("refs/tags/", string.Empty);
            names.Add(name);

            if (await GetCandidateFilePathAsync(
                repository, currentLocation.Replace('/', Path.DirectorySeparatorChar), ct) is not { } cp)
            {
                if (currentLocation.StartsWith("refs/heads/"))
                {
                    if (repository.referenceCache.Branches.TryGetValue(name, out var branchHash))
                    {
                        return new(branchHash, names.ToArray());
                    }
                }
                else if (currentLocation.StartsWith("refs/remotes/"))
                {
                    if (repository.referenceCache.RemoteBranches.TryGetValue(name, out var branchHash))
                    {
                        return new(branchHash, names.ToArray());
                    }
                }
                else if (currentLocation.StartsWith("refs/tags/") &&
                    repository.referenceCache.Tags.TryGetValue(name, out var tagReferenceHash))
                {
                    return new(tagReferenceHash.ObjectOrCommitHash, names.ToArray());
                }

                // Not found.
                return null;
            }

            using var fs = await repository.fileSystem.OpenAsync(cp.Path, false, ct);
            var tr = new AsyncTextReader(fs);

            var line = await tr.ReadLineAsync(ct);
            if (line == null)
            {
                throw new InvalidDataException(
                    $"Could not parse {currentLocation}.");
            }

            if (!line.StartsWith("ref: "))
            {
                return new(Hash.Parse(line.Trim()), names.ToArray());
            }

            currentLocation = line.Substring(5);
        }
    }

    public static Task<PrimitiveReflogEntry[]> ReadStashesAsync(
        Repository repository, CancellationToken ct) =>
        ReadReflogEntriesAsync(repository, "refs/stash", ct);
    
    public static Task<PrimitiveReflogEntry[]> ReadReflogEntriesAsync(
        Repository repository, PrimitiveReference reference, CancellationToken ct) =>
        ReadReflogEntriesAsync(repository, reference.RelativePath, ct);

    public static async Task<PrimitiveReflogEntry[]> ReadReflogEntriesAsync(
        Repository repository, string refRelativePath, CancellationToken ct)
    {
        if (await GetCandidateFilePathAsync(
            repository, repository.fileSystem.Combine("logs", refRelativePath), ct) is not { } cp)
        {
            return new PrimitiveReflogEntry[]{};
        }

        using var fs = await repository.fileSystem.OpenAsync(cp.Path, false, ct);
        var tr = new AsyncTextReader(fs);

        var entries = new List<PrimitiveReflogEntry>();
        while (true)
        {
            var line = await tr.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            if (PrimitiveReflogEntry.TryParse(line, out var reflogEntry))
            {
                entries.Add(reflogEntry);
            }
        }

        return entries.ToArray();
    }

    public static async Task<PrimitiveReference[]> ReadReferencesAsync(
        Repository repository,
        ReferenceTypes type,
        CancellationToken ct)
    {
        var candidatePaths = await GetCandidateFilePathsAsync(
            repository, repository.fileSystem.Combine("refs", GetReferenceTypeName(type)), "*", ct);
        var references = (await Utilities.WhenAll(
            candidatePaths.
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
            Select((Func<CandidateFilePath, ValueTask<PrimitiveReference?>>)(async cp =>
#else
            Select((async cp =>
#endif
            {
                if (await ReadHashAsync(
                    repository,
                    cp.Path.Substring(cp.GitPath.Length + 1),
                    ct) is not { } results)
                {
                    return default(PrimitiveReference?);
                }
                else
                {
                    return new(
                        cp.Path.Substring(cp.BasePath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        cp.Path.Substring(cp.GitPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        results.Hash);
                }
            }
            )))).
            CollectValue(reference => reference).
            ToDictionary(reference => reference.Name);

        // Branches may not all be placed in `refs/*/`.
        // Therefore, information obtained from FETCH_HEAD and packed-refs is also covered.
        switch (type)
        {
            case ReferenceTypes.Branches:
                foreach (var entry in repository.referenceCache.Branches)
                {
                    if (!references.ContainsKey(entry.Key))
                    {
                        references.Add(
                            entry.Key,
                            new(entry.Key, $"refs/heads/{entry.Key}", entry.Value));
                    }
                }
                break;
            case ReferenceTypes.RemoteBranches:
                foreach (var entry in repository.referenceCache.RemoteBranches)
                {
                    if (!references.ContainsKey(entry.Key))
                    {
                        references.Add(
                            entry.Key,
                            new(entry.Key, $"refs/remotes/{entry.Key}", entry.Value));
                    }
                }
                break;
            default:
                throw new InvalidOperationException();
        }

        return references.Values.ToArray();
    }

    public static async Task<PrimitiveTagReference[]> ReadTagReferencesAsync(
        Repository repository,
        CancellationToken ct)
    {
        var candidatePaths = await GetCandidateFilePathsAsync(
            repository, repository.fileSystem.Combine("refs", "tags"), "*", ct);
        var references = (await Utilities.WhenAll(
            candidatePaths.
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
            Select((Func<CandidateFilePath, ValueTask<PrimitiveTagReference?>>)(async cp =>
#else
            Select((async cp =>
#endif
            {
                if (await ReadHashAsync(
                    repository,
                    cp.Path.Substring(cp.GitPath.Length + 1),
                    ct) is not { } results)
                {
                    return default(PrimitiveTagReference?);
                }
                else
                {
                    return new(
                        cp.Path.Substring(cp.BasePath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        cp.Path.Substring(cp.GitPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        results.Hash,
                        null);
                }
            }
            )))).
            CollectValue(reference => reference).
            ToDictionary(reference => reference.Name);

        // Tags may not all be placed in `refs/*/`.
        // Therefore, information obtained from FETCH_HEAD and packed-refs is also covered.
        foreach (var entry in repository.referenceCache.Tags)
        {
            if (!references.ContainsKey(entry.Key))
            {
                references.Add(
                    entry.Key,
                    new(entry.Key, $"refs/tags/{entry.Key}", entry.Value.ObjectOrCommitHash, entry.Value.CommitHash));
            }
        }

        return references.Values.ToArray();
    }

    //////////////////////////////////////////////////////////////////////////

    private static ObjectAccessor GetObjectAccessor(
        Repository repository)
    {
        if (repository.objectAccessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }
        return repository.objectAccessor;
    }

    private static async Task<string?> ParseObjectBodyAsync(
        Repository repository,
        Hash hash,
        Func<ObjectTypes, bool> validateType,
        Action<string, string> gotLine,
        CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);
        if (await accessor.OpenAsync(hash, false, ct) is not { } streamResult)
        {
            return null;
        }

        try
        {
            if (!validateType(streamResult.Type))
            {
                return null;
            }

            var tr = new AsyncTextReader(streamResult.Stream);
            var body = await tr.ReadToEndAsync(ct);

            var start = 0;
            var index = 0;

            while (true)
            {
                if (index >= body.Length)
                {
                    throw new InvalidDataException(
                        $"Invalid {streamResult.Type.ToString().ToLowerInvariant()} object format: Step=1");
                }

                var ch = body[index++];
                if (ch != '\n')
                {
                    continue;
                }

                var length = index - start - 1;
                if (length == 0)
                {
                    break;
                }

                var line = body.Substring(start, length);

                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex == -1)
                {
                    throw new InvalidDataException(
                        $"Invalid {streamResult.Type.ToString().ToLowerInvariant()} object format: Step=2");
                }

                gotLine(
                    line.Substring(0, separatorIndex),
                    line.Substring(separatorIndex + 1));

                start = index;
            }

            return body.Substring(index);
        }
        finally
        {
            streamResult.Stream.Dispose();
        }
    }

    public static async Task<PrimitiveCommit?> ReadCommitAsync(
        Repository repository,
        Hash hash, CancellationToken ct)
    {
        var tree = default(Hash?);
        var parents = new List<Hash>();
        var author = default(Signature?);
        var committer = default(Signature?);

        if (await ParseObjectBodyAsync(
            repository,
            hash,
            type => type switch
            {
                ObjectTypes.Commit => true,
                _ => throw new InvalidDataException(
                    $"It isn't commit object: {hash}"),
            },
            (key, operand) =>
            {
                switch (key)
                {
                    case "tree":
                        tree = Hash.Parse(operand);
                        break;
                    case "parent":
                        parents.Add(Hash.Parse(operand));
                        break;
                    case "author":
                        author = Signature.Parse(operand);
                        break;
                    case "committer":
                        committer = Signature.Parse(operand);
                        break;
                }
            },
            ct) is not { } message)
        {
            return null;
        }

        if (tree is { } t && author is { } a && committer is { } c)
        {
            return new(hash, t, a, c, parents, message);
        }
        else
        {
            throw new InvalidDataException(
                "Invalid commit object format: Step=3");
        }
    }

    public static async Task<PrimitiveTag?> ReadTagAsync(
        Repository repository,
        Hash hash,
        CancellationToken ct)
    {
        var obj = default(Hash?);
        var objectType = default(ObjectTypes?);
        var tagName = default(string);
        var tagger = default(Signature?);

        if (await ParseObjectBodyAsync(
            repository,
            hash,
            type => type switch
            {
                ObjectTypes.Commit => false,
                ObjectTypes.Tag => true,
                _ => throw new InvalidDataException(
                    $"It isn't tag object: {hash}")
            },
            (key, operand) =>
            {
                switch (key)
                {
                    case "object":
                        obj = Hash.Parse(operand);
                        break;
                    case "type":
                        objectType = Utilities.TryParse<ObjectTypes>(operand, true, out var ot_) ?
                            ot_ : throw new InvalidDataException(
                                $"Invalid tag type: {operand}");
                        break;
                    case "tag":
                        tagName = operand;
                        break;
                    case "tagger":
                        tagger = Signature.Parse(operand);
                        break;
                }
            },
            ct) is not { } message)
        {
            return null;
        }

        if (obj is { } o && objectType is { } ot && tagName is { } tn)
        {
            return new(o, ot, tn, tagger, message);
        }
        else
        {
            throw new InvalidDataException(
                "Invalid tag object format: Step=3");
        }
    }

    public static async Task<PrimitiveTree> ReadTreeAsync(
        Repository repository,
        Hash hash,
        CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);

        // Since it is unlikely that the internally used `Stream` will be
        // reused during tree process, stream caching is disabled.
        if (await accessor.OpenAsync(hash, true, ct) is not { } streamResult)
        {
            throw new InvalidDataException(
                $"Couldn't find tree object: {hash}");
        }

        try
        {
            if (streamResult.Type != ObjectTypes.Tree)
            {
                throw new InvalidDataException(
                    $"It isn't tree object: {hash}");
            }

            var ms = new MemoryStream();
            await streamResult.Stream.CopyToAsync(ms, 4096, repository.pool, ct);

            var buffer = ms.ToArray();

            var children = new List<PrimitiveTreeEntry>();
            var index = 0;
            while (index < buffer.Length)
            {
                var start = index;
                while (true)
                {
                    if (buffer[index++] == 0x20)
                    {
                        break;
                    }

                    if (index >= buffer.Length)
                    {
                        throw new InvalidDataException(
                            $"Invalid tree object format: Step=1");
                    }
                }

                var modeFlagsString = Encoding.UTF8.GetString(
                    buffer, start, index - start - 1);
                PrimitiveModeFlags modeFlags;
                try
                {
                    modeFlags = (PrimitiveModeFlags)Convert.ToUInt16(modeFlagsString, 8);
                }
                catch
                {
                    throw new InvalidDataException(
                        $"Invalid tree object format: Step=2");
                }

                start = index;

                while (true)
                {
                    if (buffer[index++] == 0x00)
                    {
                        break;
                    }

                    if (index >= buffer.Length)
                    {
                        throw new InvalidDataException(
                            $"Invalid tree object format: Step=3");
                    }
                }

                var nameString = Encoding.UTF8.GetString(
                    buffer, start, index - start - 1);
                if (nameString.Length == 0)
                {
                    throw new InvalidDataException(
                        $"Invalid tree object format: Step=4");
                }

                if ((index + 20) > buffer.Length)
                {
                    throw new InvalidDataException(
                        $"Invalid tree object format: Step=5");
                }

                var dataHash = new Hash(buffer, index);
                index += 20;

                children.Add(new(dataHash, nameString, modeFlags));
            }

            return new(hash, children);
        }
        finally
        {
            streamResult.Stream.Dispose();
        }
    }

    public static async Task<Stream> OpenBlobAsync(
        Repository repository,
        Hash hash,
        CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);
        if (await accessor.OpenAsync(hash, true, ct) is not { } streamResult)
        {
            throw new InvalidDataException(
                $"Couldn't find tree object: {hash}");
        }

        try
        {
            if (streamResult.Type != ObjectTypes.Blob)
            {
                throw new InvalidDataException(
                    $"It isn't blob object: {hash}");
            }

            return streamResult.Stream;
        }
        catch
        {
            streamResult.Stream.Dispose();
            throw;
        }
    }

    public static async Task<ObjectStreamResult> OpenRawObjectStreamAsync(
        Repository repository,
        Hash objectId,
        CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);
        if (await accessor.OpenAsync(objectId, true, ct) is not { } streamResult)
        {
            throw new InvalidDataException(
                $"Couldn't find an object: {objectId}");
        }

        return streamResult;
    }
}
