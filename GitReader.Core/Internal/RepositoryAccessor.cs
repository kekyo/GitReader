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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

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
    public static async Task<Repository> OpenPrimitiveAsync(
        string path, CancellationToken ct, bool forceUnlock)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var lockPath = Utilities.Combine(repositoryPath, "index.lock");
        var locker = await TemporaryFile.CreateLockFileAsync(lockPath, ct, forceUnlock);

        return new(repositoryPath, locker);
    }

    private static async Task<Structures.Commit> GetCurrentHeadAsync(
        Structures.StructuredRepository repository,
        CancellationToken ct)
    {
        var results = await ReadHashAsync(
            repository, "HEAD", ct);
        var commit = await ReadCommitAsync(
            repository, results.Hash, ct);
        return new(repository, commit);
    }

    private static async Task<ReadOnlyDictionary<string, Structures.Branch>> GetStructuredBranchesAsync(
        Structures.StructuredRepository repository,
        string baseName,
        CancellationToken ct)
    {
        var references = await ReadReferencesAsync(
            repository, baseName, ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Head = await ReadCommitAsync(
                    repository, reference.Target, ct)
            }));
        return entries.ToDictionary(
            entry => entry.Name,
            entry => new Structures.Branch(entry.Name, new(repository, entry.Head))).
            AsReadOnly();
    }

    private static async Task<ReadOnlyDictionary<string, Structures.Tag>> GetStructuredTagsAsync(
        Structures.StructuredRepository repository,
        CancellationToken ct)
    {
        var references = await ReadReferencesAsync(
            repository, "tags", ct);
        var entries = await Utilities.WhenAll(
            references.Select(async reference =>
            new
            {
                Name = reference.Name,
                Tag = (await ReadTagAsync(
                    repository, reference.Target, ct)) is { } tag ?
                        new Structures.Tag(tag) :
                        new Structures.Tag(reference.Target, ObjectTypes.Commit, reference.Name),
            }));
        return entries.ToDictionary(
            entry => entry.Name,
            entry => entry.Tag).
            AsReadOnly();
    }

    public static async Task<Structures.StructuredRepository> OpenStructuredAsync(
        string path, CancellationToken ct, bool forceUnlock)
    {
        var repositoryPath = Path.GetFileName(path) != ".git" ?
            Utilities.Combine(path, ".git") : path;

        if (!Directory.Exists(repositoryPath))
        {
            throw new ArgumentException("Repository does not exist.");
        }

        var lockPath = Utilities.Combine(repositoryPath, "index.lock");
        var locker = await TemporaryFile.CreateLockFileAsync(lockPath, ct, forceUnlock);

        try
        {
            var repository = new Structures.StructuredRepository(repositoryPath, locker);

            var (head, branches, remoteBranches, tags) = await Utilities.WhenAll(
                GetCurrentHeadAsync(repository, ct),
                GetStructuredBranchesAsync(repository, "heads", ct),
                GetStructuredBranchesAsync(repository, "remotes", ct),
                GetStructuredTagsAsync(repository, ct));

            repository.head = head;
            repository.branches = branches;
            repository.remoteBranches = remoteBranches;
            repository.tags = tags;

            return repository;
        }
        catch
        {
            locker.Dispose();
            throw;
        }
    }

    //////////////////////////////////////////////////////////////////////////

    public static async Task<Reference[]> ReadReferencesAsync(
        Repository repository,
        string type,
        CancellationToken ct)
    {
        var headsPath = Utilities.Combine(
            repository.Path, "refs", type);
        var branches = await Utilities.WhenAll(
            Utilities.EnumerateFiles(headsPath, "*").
            Select(async path =>
            {
                var results = await ReadHashAsync(
                    repository,
                    path.Substring(repository.Path.Length + 1),
                    ct);
                return Reference.Create(
                    path.Substring(headsPath.Length + 1).Replace(Path.DirectorySeparatorChar, '/'),
                    results.Hash);
            }));
        return branches;
    }

    public static async Task<HashResults> ReadHashAsync(
        Repository repository,
        string relativePath,
        CancellationToken ct)
    {
        var currentPath = relativePath;
        var names = new List<string>();

        while (true)
        {
            var name = string.Join("/", currentPath.Split(
                new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).
                Skip(2).
                ToArray());
            names.Add(name);

            var path = Utilities.Combine(
                repository.Path,
                currentPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new InvalidDataException(
                    $"Could not find {currentPath}.");
            }

            using var fs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var tr = new StreamReader(fs, Encoding.UTF8, true);

            var line = await tr.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                throw new InvalidDataException(
                    $"Could not parse {currentPath}.");
            }

            if (!line.StartsWith("ref: "))
            {
                return new(Hash.Parse(line.Trim()), names.ToArray());
            }

            currentPath = line.Substring(5);
        }
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

    public static async Task<Commit> ReadCommitAsync(
        Repository repository,
        Hash hash, CancellationToken ct)
    {
        var streamResult = await repository.accessor.OpenAsync(hash, ct);

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
        var streamResult = await repository.accessor.OpenAsync(hash, ct);

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
