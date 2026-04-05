using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// Postgres-backed single-record store. Session is bound at query time by PostgresMemory.
/// Use for settings, config, or any singleton-style state.
/// </summary>
public sealed class PostgresVault<TResult> : IVault<TResult> where TResult : notnull
{
    private static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid VersionId   = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly IDocumentSession session;
    private uint loadedVersion;

    internal PostgresVault(IDocumentSession session) => this.session = session;

    public async Task<OneOf<TResult, NotFound>> Load()
    {
        var record     = await session.LoadAsync<TResult>(SingletonId);
        var versionDoc = await session.LoadAsync<ApiaVersion>(VersionId);
        loadedVersion  = versionDoc?.Version ?? 0u;
        OneOf<TResult, NotFound> result = record is null
            ? new NotFound()
            : record;
        return result;
    }

    public async Task Save(TResult record)
    {
        var versionDoc     = await session.LoadAsync<ApiaVersion>(VersionId);
        var currentVersion = versionDoc?.Version ?? 0u;
        if (HasConflict(currentVersion, loadedVersion))
            throw new ConcurrentModificationException(typeof(TResult), SingletonId);
        session.Store(record);
        session.Store(new ApiaVersion(VersionId, typeof(TResult).Name, SingletonId, currentVersion + 1));
    }

    private static bool HasConflict(uint currentVersion, uint loadedVersion) =>
        currentVersion > 0 && currentVersion != loadedVersion;
}
