using System.Collections.Concurrent;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// A Postgres-backed entity origin. Subclass and implement IdOf()
/// to provide the id selector. Register via PostgresMemoryMap.Register(instance).
/// </summary>
public abstract class PostgresEntities<TRecord>
    : IEntitiesOrigin<TRecord, (IMemory Memory, IDocumentSession Session)>
    where TRecord : notnull
{
    /// <summary>Extracts the Guid key from a record.</summary>
    protected abstract Guid IdOf(TRecord record);

    /// <inheritdoc/>
    public IEntities<TRecord> Bind((IMemory Memory, IDocumentSession Session) context)
        => new BoundEntities(context.Session, IdOf);

    /// <summary>Session-bound entity store, created at query time.</summary>
    private sealed class BoundEntities(IDocumentSession session, Func<TRecord, Guid> idOf)
        : IEntities<TRecord>
    {
        private readonly ConcurrentDictionary<Guid, uint> loadedVersions = new();

        /// <inheritdoc/>
        public Guid IdOf(TRecord record) => idOf(record);

        /// <inheritdoc/>
        public async IAsyncEnumerable<TRecord> All()
        {
            await foreach (var record in session.Query<TRecord>().ToAsyncEnumerable())
                yield return record;
        }

        /// <inheritdoc/>
        public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
        {
            var record = await session.LoadAsync<TRecord>(id);
            if (record is null)
                return new NotFound();
            var version = await LoadVersion(id);
            loadedVersions[id] = version;
            return record;
        }

        /// <inheritdoc/>
        public async Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
        {
            var id              = idOf(record);
            var currentVersion  = await LoadVersion(id);
            var expectedVersion = loadedVersions.GetValueOrDefault(id, 0u);
            if (currentVersion > 0 && currentVersion != expectedVersion)
            {
                var current  = await session.LoadAsync<TRecord>(id);
                var conflict = new Conflict<TRecord>(current!, record);
                return OneOf<TRecord, Conflict<TRecord>>.FromT1(conflict);
            }
            session.Store(record);
            session.Store(new ApiaVersion(VersionId(id), typeof(TRecord).Name, id, currentVersion + 1));
            return OneOf<TRecord, Conflict<TRecord>>.FromT0(record);
        }

        /// <inheritdoc/>
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
}
