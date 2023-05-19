using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitReader.Internal;

namespace GitReader.Structures;

public abstract class CommitRef
{
    private readonly WeakReference rwr;
    public readonly Hash CommitHash;

    internal CommitRef(WeakReference rwr, Hash hash)
    {
        this.rwr = rwr;
        CommitHash = hash;
    }

    public async Task<Commit> GetCommitAsync(CancellationToken ct = default)
    {
        var repository = GetRelatedRepository();
        var pc = await RepositoryAccessor.ReadCommitAsync(repository, CommitHash, ct);
        return pc is { } ?
            new(new (repository), pc!.Value) :
            throw new InvalidDataException(
                $"Could not find a commit: {CommitHash}");
    }

    public StructuredRepository GetRelatedRepository()
    {
        if (rwr.Target is not StructuredRepository repository || repository.objectAccessor == null)
        {
            throw new InvalidOperationException(
                "The repository already discarded.");
        }

        return repository;
    }
}