////////////////////////////////////////////////////////////////////////////
//
// GitReader - Lightweight Git local repository traversal library.
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using GitReader.Collections;
using GitReader.Internal;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.Structures;

public static class RepositoryExtension
{
    public static Branch? GetCurrentHead(
        this StructuredRepository repository) =>
        repository.head;

    public static Task<Commit?> GetCommitAsync(
        this StructuredRepository repository,
        Hash commit, CancellationToken ct = default) =>
        RepositoryFacade.GetCommitDirectlyAsync(repository, commit, ct);

    public static Task<Commit> GetHeadCommitAsync(
        this Branch branch,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCommitAsync(branch, ct);

    public static Task<Commit> GetCommitAsync(
        this CommitTag tag,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCommitAsync(tag, ct);

    public static Task<Commit> GetCommitAsync(
        this Stash stash,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCommitAsync(stash, ct);

    public static Task<Commit> GetCurrentCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCommitAsync(reflog, reflog.Commit, ct);

    public static Task<Commit> GetOldCommitAsync(
        this ReflogEntry reflog,
        CancellationToken ct = default) =>
        RepositoryFacade.GetCommitAsync(reflog, reflog.OldCommit, ct);

    public static Task<Commit?> GetPrimaryParentCommitAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        RepositoryFacade.GetPrimaryParentAsync(commit, ct);

    public static Task<Commit[]> GetParentCommitsAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        RepositoryFacade.GetParentsAsync(commit, ct);

    public static Task<TreeRoot> GetTreeRootAsync(
        this Commit commit,
        CancellationToken ct = default) =>
        RepositoryFacade.GetTreeAsync(commit, ct);

    public static Task<Stream> OpenBlobAsync(
        this TreeBlobEntry entry,
        CancellationToken ct = default) =>
        RepositoryFacade.OpenBlobAsync(entry, ct);

    public static string GetMessage(
        this Commit commit) =>
        commit.message;
    
    public static Task<ReflogEntry[]> GetHeadReflogsAsync(
        this StructuredRepository repository, CancellationToken ct = default) =>
        RepositoryFacade.GetHeadReflogsAsync(
            repository, new WeakReference(repository), ct);

    public static void Deconstruct(
        this StructuredRepository repository,
        out string gitPath,
        out ReadOnlyDictionary<string, string> remoteUrls,
        out Branch? head,
        out ReadOnlyDictionary<string, Branch> branches,
        out ReadOnlyDictionary<string, Branch> remoteBranches,
        out ReadOnlyDictionary<string, Tag> tags)
    {
        gitPath = repository.GitPath;
        remoteUrls = repository.RemoteUrls;
        head = repository.head;
        branches = repository.branches;
        remoteBranches = repository.remoteBranches;
        tags = repository.tags;
    }

    public static void Deconstruct(
        this Branch branch,
        out string name,
        out Hash head)
    {
        name = branch.Name;
        head = branch.Head;
    }

    public static void Deconstruct(
        this Commit commit,
        out Hash hash,
        out Signature author,
        out Signature committer,
        out string subject,
        out string body)
    {
        hash = commit.Hash;
        author = commit.Author;
        committer = commit.Committer;
        subject = commit.Subject;
        body = commit.Body;
    }

    public static void Deconstruct(
        this Commit commit,
        out Hash hash,
        out Signature author,
        out Signature committer,
        out string message)
    {
        hash = commit.Hash;
        author = commit.Author;
        committer = commit.Committer;
        message = commit.message;
    }

    public static void Deconstruct(
        this Tag tag,
        out Hash hash,
        out string name,
        out Signature? tagger,
        out string? message)
    {
        hash = tag.Hash;
        name = tag.Name;
        tagger = tag.Tagger;
        message = tag.Message;
    }

    public static void Deconstruct(
        this Tag tag,
        out Hash hash,
        out string name,
        out ObjectTypes type,
        out Signature? tagger,
        out string? message)
    {
        hash = tag.Hash;
        name = tag.Name;
        type = tag.Type;
        tagger = tag.Tagger;
        message = tag.Message;
    }

    public static void Deconstruct(
        this CommitTag tag,
        out Hash hash,
        out string name,
        out Signature? tagger,
        out string? message)
    {
        hash = tag.Hash;
        name = tag.Name;
        tagger = tag.Tagger;
        message = tag.Message;
    }
}
