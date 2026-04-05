using System.Collections.Concurrent;
using Marten;

namespace Apia.Postgres;

/// <summary>Session-bound catalog — created by PostgresMemory at query time.</summary>
public sealed class BoundPostgresEntities<TRecord> : IEntities<TRecord> where TRecord : notnull
{
    private readonly IDocumentSession session;
    private readonly Func<TRecord, Guid> idOf;
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

    internal BoundPostgresEntities(IDocumentSession session, Func<TRecord, Guid> idOf)
    {
        this.session = session;
        this.idOf    = idOf;
    }

    public Guid IdOf(TRecord record)  => idOf(record);

    public async IAsyncEnumerable<TRecord> All()
    {
        await foreach (var record in session.Query<TRecord>().ToAsyncEnumerable())
            yield return record;
    }

    public async Task<TRecord> Fetch(Guid id)
    {
        var record = await session.LoadAsync<TRecord>(id);
        if (record is null)
            throw new KeyNotFoundException($"No {typeof(TRecord).Name} found with id {id}.");
        var version = await LoadVersion(id);
        loadedVersions[id] = version;
        return record;
    }

    public async Task Save(TRecord record)
    {
        var id              = idOf(record);
        var currentVersion  = await LoadVersion(id);
        var expectedVersion = loadedVersions.GetValueOrDefault(id, 0u);
        if (currentVersion > 0 && currentVersion != expectedVersion)
            throw new ConcurrentModificationException(typeof(TRecord), id);
        session.Store(record);
        session.Store(new ApiaVersion(VersionId(id), typeof(TRecord).Name, id, currentVersion + 1));
    }

    public Task Delete(Guid id)
    {
        session.Delete<TRecord>(id);
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
        var bytes = System.Text.Encoding.UTF8.GetBytes(typeof(TRecord).Name)
            .Concat(recordId.ToByteArray())
            .ToArray();
        return new Guid(System.Security.Cryptography.MD5.HashData(bytes));
    }
}
