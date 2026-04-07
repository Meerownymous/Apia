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

    public async Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
    {
        var versionDoc     = await session.LoadAsync<ApiaVersion>(VersionId);
        var currentVersion = versionDoc?.Version ?? 0u;
        if (currentVersion > 0 && currentVersion != loadedVersion)
        {
            var current = await session.LoadAsync<TResult>(SingletonId);
            var conflict = new Conflict<TResult>(current!, record);
            return OneOf<TResult, Conflict<TResult>>.FromT1(conflict);
        }
        session.Store(record);
        session.Store(new ApiaVersion(typeof(TResult).Name, SingletonId, currentVersion + 1));
        return OneOf<TResult, Conflict<TResult>>.FromT0(record);
    }
}
