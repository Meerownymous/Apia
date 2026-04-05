using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// Postgres-backed single-record store. Session is bound at query time by PostgresMemory.
/// Use for settings, config, or any singleton-style state.
/// </summary>
public sealed class PostgresMutable<TResult> : IMutable<TResult> where TResult : notnull
{
    private static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid VersionId   = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly IDocumentSession session;
    private uint loadedVersion;

    internal PostgresMutable(IDocumentSession session) => this.session = session;

    public async Task<OneOf<TResult, NotFound>> Load()
    {
        var record     = await session.LoadAsync<TResult>(SingletonId);
        var versionDoc = await session.LoadAsync<ApiaVersion>(VersionId);
        loadedVersion  = versionDoc?.Version ?? 0u;
        var result = record is null
            ? OneOf<TResult, NotFound>.FromT1(new NotFound())
            : OneOf<TResult, NotFound>.FromT0(record);
        return result;
    }

    public async Task Save(TResult record)
    {
        var versionDoc     = await session.LoadAsync<ApiaVersion>(VersionId);
        var currentVersion = versionDoc?.Version ?? 0u;
        if (currentVersion > 0 && currentVersion != loadedVersion)
            throw new ConcurrentModificationException(typeof(TResult), SingletonId);
        session.Store(record);
        session.Store(new ApiaVersion(VersionId, typeof(TResult).Name, SingletonId, currentVersion + 1));
    }
}
