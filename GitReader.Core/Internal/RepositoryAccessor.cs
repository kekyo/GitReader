////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
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
    Tags,
}

internal readonly struct ReferenceCache
{
    public readonly ReadOnlyDictionary<string, Hash> Branches;
    public readonly ReadOnlyDictionary<string, Hash> RemoteBranches;
    public readonly ReadOnlyDictionary<string, Hash> Tags;

    public ReferenceCache(
        ReadOnlyDictionary<string, Hash> branches,
        ReadOnlyDictionary<string, Hash> remoteBranches,
        ReadOnlyDictionary<string, Hash> tags)
    {
        this.Branches = branches;
        this.RemoteBranches = remoteBranches;
        this.Tags = tags;
    }

    public ReferenceCache Combine(ReferenceCache rhs)
    {
        var branches = this.RemoteBranches.Clone();
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
    public static string GetReferenceTypeName(ReferenceTypes type) =>
        type switch
        {
            ReferenceTypes.Branches => "heads",
            ReferenceTypes.RemoteBranches => "remotes",
            ReferenceTypes.Tags => "tags",
            _ => throw new ArgumentException(),
        };

    public static async Task<ReadOnlyDictionary<string, string>> ReadRemoteReferencesAsync(
        Repository repository,
        CancellationToken ct)
    {
        var path = Utilities.Combine(repository.GitPath, "config");
        if (!File.Exists(path))
        {
            return new(new());
        }

        using var fs = repository.fileAccessor.Open(path);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var remotes = new Dictionary<string, string>();
        var remoteName = default(string);

        while (true)
        {
            var line = await tr.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                break;
            }

            line = line.Trim();

            switch (remoteName)
            {
                case null:
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
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    if (line.StartsWith("url"))
                    {
                        if (line.Split('=').ElementAtOrDefault(1)?.Trim() is { } urlString)
                        {
                            remotes[remoteName] = urlString;
                        }
                    }
                    break;
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

        var path = Utilities.Combine(repository.GitPath, "FETCH_HEAD");
        if (!File.Exists(path))
        {
            return new(new(new()), new(new()), new(new()));
        }

        using var fs = repository.fileAccessor.Open(path);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var remoteBranches = new Dictionary<string, Hash>();
        var tags = new Dictionary<string, Hash>();

        while (true)
        {
            var line = await tr.ReadLineAsync().WaitAsync(ct);
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
                    tags[name] = hash;
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
                    tags[descriptorString] = hash;
                }
            }
        }

        return new(new(new()), remoteBranches, tags);
    }

    public static async Task<ReferenceCache> ReadPackedRefsAsync(
        Repository repository,
        CancellationToken ct)
    {
        var path = Utilities.Combine(repository.GitPath, "packed-refs");
        if (!File.Exists(path))
        {
            return new(new(new()), new(new()), new(new()));
        }

        using var fs = repository.fileAccessor.Open(path);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var branches = new Dictionary<string, Hash>();
        var remoteBranches = new Dictionary<string, Hash>();
        var tags = new Dictionary<string, Hash>();

        var separators = new[] { ' ' };

        while (true)
        {
            var line = await tr.ReadLineAsync().WaitAsync(ct);
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

            // Ignored peeled tag entry.
            if (columns.Length == 1 &&
                hashCodeString.StartsWith("^"))
            {
                continue;
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
                var name = referenceString.Substring("refs/tags/".Length);
                tags[name] = hash;
            }
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

            var path = Utilities.Combine(
                repository.GitPath,
                currentLocation.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                if (currentLocation.StartsWith("refs/remotes/"))
                {
                    if (repository.referenceCache.RemoteBranches.TryGetValue(name, out var branchHash))
                    {
                        return new(branchHash, names.ToArray());
                    }
                }
                else if (currentLocation.StartsWith("refs/tags/") &&
                    repository.referenceCache.Tags.TryGetValue(name, out var tagHash))
                {
                    return new(tagHash, names.ToArray());
                }

                // Not found.
                return null;
            }

            using var fs = repository.fileAccessor.Open(path);
            var tr = new StreamReader(fs, Encoding.UTF8, true);

            var line = await tr.ReadLineAsync().WaitAsync(ct);
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

    public static Task<PrimitiveRefLogEntry[]> ReadStashesAsync(Repository repository, CancellationToken ct)
        => ReadRefLogAsync(repository, "refs/stash", ct);
    
    public static Task<PrimitiveRefLogEntry[]> ReadRefLogAsync(Repository repository, PrimitiveReference reference, CancellationToken ct)
        => ReadRefLogAsync(repository, reference.RelativePath, ct);

    public static async Task<PrimitiveRefLogEntry[]> ReadRefLogAsync(Repository repository, string refRelativePath, CancellationToken ct)
    {
        var path = Utilities.Combine(repository.GitPath, "logs", refRelativePath);
        if (!File.Exists(path))
        {
            return new PrimitiveRefLogEntry[]{};
        }

        using var fs = repository.fileAccessor.Open(path);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var entries = new List<PrimitiveRefLogEntry>();
        while (true)
        {
            var line = await tr.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                break;
            }

            if (PrimitiveRefLogEntry.TryParse(line, out var refLogEntry))
            {
                entries.Add(refLogEntry);
            }
        }

        return entries.ToArray();
    }

    public static async Task<PrimitiveReference[]> ReadReferencesAsync(
        Repository repository,
        ReferenceTypes type,
        CancellationToken ct)
    {
        var headsPath = Utilities.Combine(
            repository.GitPath, "refs", GetReferenceTypeName(type));
        var references = (await Utilities.WhenAll(
            Utilities.EnumerateFiles(headsPath, "*").
            Select(async path =>
            {
                if (await ReadHashAsync(
                    repository,
                    path.Substring(repository.GitPath.Length + 1),
                    ct) is not { } results)
                {
                    return default(PrimitiveReference?);
                }
                else
                {
                    return new(
                        path.Substring(headsPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        path.Substring(repository.GitPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        results.Hash);
                }
            }))).
            CollectValue(reference => reference).
            ToDictionary(reference => reference.Name);

        // Remote branches and tags may not all be placed in `refs/*/`.
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
                            new(entry.Key,$"refs/heads/{entry.Key}", entry.Value));
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
                            new(entry.Key,$"refs/remotes/{entry.Key}", entry.Value));
                    }
                }
                break;
            case ReferenceTypes.Tags:
                foreach (var entry in repository.referenceCache.Tags)
                {
                    if (!references.ContainsKey(entry.Key))
                    {
                        references.Add(
                            entry.Key,
                            new(entry.Key, $"refs/tags/{entry.Key}", entry.Value));
                    }
                }
                break;
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

            var tr = new StreamReader(streamResult.Stream, Encoding.UTF8, true);
            var body = await tr.ReadToEndAsync().WaitAsync(ct);

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
            return new(hash, t, a, c, parents.ToArray(), message);
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
            await streamResult.Stream.CopyToAsync(ms, 4096, ct);

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

            return new(hash, children.ToArray());
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
}
