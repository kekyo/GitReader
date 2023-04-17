////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository exploration library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Internal;

internal static class RepositoryExtension
{
    public readonly struct HashResults
    {
        public readonly Hash Hash;
        public readonly string[] Names;

        public HashResults(Hash hash, string[] names)
        {
            this.Hash = hash;
            this.Names = names;
        }
    }

    public static async Task<HashResults> ReadHashAsync(
        this Repository repository,
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
                throw new FormatException(
                    $"Could not find {currentPath}.");
            }

            using var fs = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            var tr = new StreamReader(fs, Encoding.UTF8, true);

            var line = await tr.ReadLineAsync().WaitAsync(ct);
            if (line == null)
            {
                throw new FormatException(
                    $"Could not parse {currentPath}.");
            }

            if (!line.StartsWith("ref: "))
            {
                return new(Hash.Parse(line.Trim()), names.ToArray());
            }

            currentPath = line.Substring(5);
        }
    }

    public static string GetObjectPath(
        this Repository repository,
        Hash hash) =>
        Utilities.Combine(
            repository.Path,
            "objects",
            BitConverter.ToString(hash.HashCode, 0, 1).ToLowerInvariant(),
            BitConverter.ToString(hash.HashCode, 1).Replace("-", string.Empty).ToLowerInvariant());

    public static async Task<Commit> ReadCommitAsync(
        this Repository repository,
        Hash hash, CancellationToken ct)
    {
        Stream stream;
        TextReader tr;

        var path = repository.GetObjectPath(hash);
        if (File.Exists(path))
        {
            stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
            try
            {
                var os = await ObjectStream.CreateAsync(stream, hash, ct);
                if (os.Type != "commit")
                {
                    throw new FormatException(
                        $"It isn't commit object: {hash}");
                }
                tr = new StreamReader(os, Encoding.UTF8, true);
            }
            finally
            {
                stream.Dispose();
            }
        }
        else
        {
            var basePath = Utilities.Combine(
                repository.Path,
                "objects",
                "pack");
            stream = await PackedObjectStream.CreateAsync(basePath, hash, ct);
            tr = new StreamReader(stream, Encoding.UTF8, true);
        }

        try
        {
            var tree = default(Hash?);
            var parents = new List<Hash>();
            var author = default(Signature?);
            var committer = default(Signature?);

            while (true)
            {
                var line = await tr.ReadLineAsync().WaitAsync(ct);
                if (line == null)
                {
                    throw new FormatException(
                        "Invalid commit object format: Step=1");
                }

                if (line.Length == 0)
                {
                    break;
                }

                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex == -1)
                {
                    throw new FormatException(
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

                return Commit.Create(hash, t, a, c, parents.ToArray(), sb.ToString());
            }
            else
            {
                throw new FormatException(
                    "Invalid commit object format: Step=3");
            }
        }
        finally
        {
            stream.Dispose();
        }
    }
}
