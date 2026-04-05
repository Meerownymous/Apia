using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>
/// Postgres-backed catalog. Holds only the id selector — session is bound at query time.
/// Usage: new PostgresEntities&lt;PostRecord&gt;(p => p.PostId)
/// </summary>
public sealed class PostgresEntities<TResult>(Func<TResult, Guid> idOf) : IEntities<TResult>
    where TResult : notnull
{
    internal BoundPostgresEntities<TResult> Bind(IDocumentSession session)
        => new(session, idOf);

    public Func<TResult, Guid> IdOf
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public IAsyncEnumerable<Guid> Ids()
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task<TResult> Load(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task Save(TResult record)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task Delete(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");
}

/// <summary>Session-bound catalog — created by PostgresMemory at query time.</summary>
internal sealed class BoundPostgresEntities<TResult> : IEntities<TResult> where TResult : notnull
{
    private readonly IDocumentSession session;
    private readonly Func<TResult, Guid> idOf;
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

    internal BoundPostgresEntities(IDocumentSession session, Func<TResult, Guid> idOf)
    {
        this.session = session;
        this.idOf    = idOf;
    }

    public Func<TResult, Guid> IdOf => idOf;

    public async IAsyncEnumerable<Guid> Ids()
    {
        var records = await session.Query<TResult>().ToListAsync();
        foreach (var record in records)
            yield return idOf(record);
    }

    public async Task<TResult> Load(Guid id)
    {
        var record = await session.LoadAsync<TResult>(id);
        if (record is null)
            throw new KeyNotFoundException($"No {typeof(TResult).Name} found with id {id}.");
        var version = await LoadVersion(id);
        loadedVersions[id] = version;
        return record;
    }

    public async Task Save(TResult record)
    {
        var id              = idOf(record);
        var currentVersion  = await LoadVersion(id);
        var expectedVersion = loadedVersions.GetValueOrDefault(id, 0u);
        if (currentVersion > 0 && currentVersion != expectedVersion)
            throw new ConcurrentModificationException(typeof(TResult), id);
        session.Store(record);
        session.Store(new ApiaVersion(VersionId(id), typeof(TResult).Name, id, currentVersion + 1));
    }

    public Task Delete(Guid id)
    {
        session.Delete<TResult>(id);
        session.Delete<ApiaVersion>(VersionId(id));
        loadedVersions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private async Task<uint> LoadVersion(Guid id)
    {
        var doc = await session.LoadAsync<ApiaVersion>(VersionId(id));
        return doc?.Version ?? 0u;
    }

    private static Guid VersionId(Guid recordId)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(typeof(TResult).Name)
            .Concat(recordId.ToByteArray())
            .ToArray();
        return new Guid(System.Security.Cryptography.MD5.HashData(bytes));
    }
}
