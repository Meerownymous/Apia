using System.Collections.Concurrent;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>Session-bound catalog — created by PostgresMemory at query time.</summary>
public sealed class BoundPostgresEntities<TRecord> : IEntities<TRecord> where TRecord : notnull
{
    private readonly IDocumentSession session;
    private readonly Func<TRecord, Guid> idOf;
    private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

    public BoundPostgresEntities(IDocumentSession session, Func<TRecord, Guid> idOf)
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

    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var record = await session.LoadAsync<TRecord>(id);
        if (record is null)
            return new NotFound();
        var version = await LoadVersion(id);
        loadedVersions[id] = version;
        return record;
    }

    public async Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
        var id              = idOf(record);
        var currentVersion  = await LoadVersion(id);
        var expectedVersion = loadedVersions.GetValueOrDefault(id, 0u);
        if (currentVersion > 0 && currentVersion != expectedVersion)
        {
            var current = await session.LoadAsync<TRecord>(id);
            var conflict = new Conflict<TRecord>(current!, record);
            return OneOf<TRecord, Conflict<TRecord>>.FromT1(conflict);
        }
        session.Store(record);
        session.Store(new ApiaVersion(VersionId(id), typeof(TRecord).Name, id, currentVersion + 1));
        return OneOf<TRecord, Conflict<TRecord>>.FromT0(record);
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
