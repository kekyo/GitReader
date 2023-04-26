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

internal sealed class RemoteReferenceCache
{
    public readonly ReadOnlyDictionary<Uri, string> Remotes;

    public RemoteReferenceCache(ReadOnlyDictionary<Uri, string> remotes) =>
        this.Remotes = remotes;
}

internal sealed class FetchHeadCache
{
    public readonly ReadOnlyDictionary<string, Hash> RemoteBranches;
    public readonly ReadOnlyDictionary<string, Hash> Tags;

    public FetchHeadCache(
        ReadOnlyDictionary<string, Hash> remoteBranches,
        ReadOnlyDictionary<string, Hash> tags)
    {
        this.RemoteBranches = remoteBranches;
        this.Tags = tags;
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
    private static async Task<RemoteReferenceCache> GetRemotesAsync(
        Repository repository,
        CancellationToken ct)
    {
        var path = Utilities.Combine(repository.Path, "config");
        if (!File.Exists(path))
        {
            return new(new(new()));
        }

        using var fs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var remotes = new Dictionary<Uri, string>();
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
                        if (line.Split('=').ElementAtOrDefault(1)?.Trim() is { } urlString &&
                            Uri.TryCreate(urlString, UriKind.Absolute, out var url))
                        {
                            remotes[url] = remoteName;
                        }
                    }
                    break;
            }
        }

        return new(remotes);
    }

    private static async Task<FetchHeadCache> GetFetchHeadsAsync(
        Repository repository,
        CancellationToken ct)
    {
        var remoteReferenceCache = repository.remoteReferenceCache;

        Debug.Assert(remoteReferenceCache != null);

        var path = Utilities.Combine(repository.Path, "FETCH_HEAD");
        if (!File.Exists(path))
        {
            return new(new(new()), new(new()));
        }

        using var fs = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var tr = new StreamReader(fs, Encoding.UTF8, true);

        var branches = new Dictionary<string, Hash>();
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

            // FETCH_HEAD file is a file log, so will be overwrite dictionary entry.

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
                    if (Uri.TryCreate(urlString, UriKind.Absolute, out var url) &&
                        remoteReferenceCache!.Remotes.TryGetValue(url, out var remoteName))
                    {
                        branches[$"{remoteName}/{name}"] = hash;
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
                    branches[descriptorString] = hash;
                }
                else
                {
                    tags[descriptorString] = hash;
                }
            }
        }

        return new(branches, tags);
    }

    public static async Task<HashResults?> ReadHashAsync(
        Repository repository,
        string relativePath,
        CancellationToken ct)
    {
        var currentLocation = relativePath.
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
                repository.Path,
                currentLocation.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                // Read remotes from config file.
                if (repository.remoteReferenceCache == null)
                {
                    repository.remoteReferenceCache = await GetRemotesAsync(repository, ct);
                }

                // Lookup by FETCH_HEAD cache.
                var fetchHeadCache = repository.fetchHeadCache;
                if (fetchHeadCache == null)
                {
                    repository.fetchHeadCache = await GetFetchHeadsAsync(repository, ct);
                    fetchHeadCache = repository.fetchHeadCache;
                }

                if (currentLocation.StartsWith("refs/remotes/"))
                {
                    if (fetchHeadCache.RemoteBranches.TryGetValue(name, out var branchHash))
                    {
                        return new(branchHash, names.ToArray());
                    }
                }
                else if (currentLocation.StartsWith("refs/tags/") &&
                    fetchHeadCache.Tags.TryGetValue(name, out var tagHash))
                {
                    return new(tagHash, names.ToArray());
                }

                // Not found.
                return null;
            }

            using var fs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
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

    public static async Task<Reference[]> ReadReferencesAsync(
        Repository repository,
        string type,
        CancellationToken ct)
    {
        var headsPath = Utilities.Combine(
            repository.Path, "refs", type);
        var references = await Utilities.WhenAll(
            Utilities.EnumerateFiles(headsPath, "*").
            Select(async path =>
            {
                if (await ReadHashAsync(
                    repository,
                    path.Substring(repository.Path.Length + 1),
                    ct) is not { } results)
                {
                    return default(Reference?);
                }
                else
                {
                    return Reference.Create(
                        path.Substring(headsPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                        results.Hash);
                }
            }));
        return references.CollectValue(reference => reference).
            ToArray();
    }

    //////////////////////////////////////////////////////////////////////////

    private static async Task<string> GetMessageAsync(
        TextReader tr, CancellationToken ct)
    {
        var sb = new StringBuilder();

        while (true)
        {
            var line = await tr.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                break;
            }

            if (sb.Length >= 1)
            {
                sb.Append('\n');   // Make deterministic
            }
            sb.Append(line);
        }

        return sb.ToString();
    }

    private static ObjectAccessor GetObjectAccessor(
        Repository repository)
    {
        if (repository.accessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }
        return repository.accessor;
    }

    public static async Task<Commit?> ReadCommitAsync(
        Repository repository,
        Hash hash, CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);
        if (await accessor.OpenAsync(hash, ct) is not { } streamResult)
        {
            return null;
        }

        try
        {
            if (streamResult.Type != ObjectTypes.Commit)
            {
                throw new InvalidDataException(
                    $"It isn't commit object: {hash}");
            }

            var tr = new StreamReader(streamResult.Stream, Encoding.UTF8, true);

            var tree = default(Hash?);
            var parents = new List<Hash>();
            var author = default(Signature?);
            var committer = default(Signature?);

            while (true)
            {
                var line = await tr.ReadLineAsync().WaitAsync(ct);
                if (line == null)
                {
                    throw new InvalidDataException(
                        "Invalid commit object format: Step=1");
                }

                if (line.Length == 0)
                {
                    break;
                }

                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex == -1)
                {
                    throw new InvalidDataException(
                        "Invalid commit object format: Step=2");
                }

                var type = line.Substring(0, separatorIndex);
                var operand = line.Substring(separatorIndex + 1);

                switch (type)
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

            }

            if (tree is { } t && author is { } a && committer is { } c)
            {
                var message = await GetMessageAsync(tr, ct);

                return Commit.Create(hash, t, a, c, parents.ToArray(), message);
            }
            else
            {
                throw new InvalidDataException(
                    "Invalid commit object format: Step=3");
            }
        }
        finally
        {
            streamResult.Stream.Dispose();
        }
    }

    public static async Task<Tag?> ReadTagAsync(
        Repository repository,
        Hash hash, CancellationToken ct)
    {
        var accessor = GetObjectAccessor(repository);
        if (await accessor.OpenAsync(hash, ct) is not { } streamResult)
        {
            return null;
        }

        try
        {
            // Lightweight tag
            if (streamResult.Type == ObjectTypes.Commit)
            {
                return null;
            }

            if (streamResult.Type != ObjectTypes.Tag)
            {
                throw new InvalidDataException(
                    $"It isn't tag object: {hash}");
            }

            var tr = new StreamReader(streamResult.Stream, Encoding.UTF8, true);

            var obj = default(Hash?);
            var objectType = default(ObjectTypes?);
            var tagName = default(string);
            var tagger = default(Signature?);

            while (true)
            {
                var line = await tr.ReadLineAsync().WaitAsync(ct);
                if (line == null)
                {
                    throw new InvalidDataException(
                        "Invalid tag object format: Step=1");
                }

                if (line.Length == 0)
                {
                    break;
                }

                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex == -1)
                {
                    throw new InvalidDataException(
                        "Invalid tag object format: Step=2");
                }

                var type = line.Substring(0, separatorIndex);
                var operand = line.Substring(separatorIndex + 1);

                switch (type)
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

            }

            if (obj is { } o && objectType is { } ot && tagName is { } tn && tagger is { } tg)
            {
                var message = await GetMessageAsync(tr, ct);

                return Tag.Create(hash, ot, tn, tg, message);
            }
            else
            {
                throw new InvalidDataException(
                    "Invalid commit object format: Step=3");
            }
        }
        finally
        {
            streamResult.Stream.Dispose();
        }
    }
}
