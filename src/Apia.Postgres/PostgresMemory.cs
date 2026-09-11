using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Postgres-backed IMemory via a Marten IDocumentStore. Sessions are created per access.</summary>
public sealed class PostgresMemory(
    IDocumentStore store,
    ConcurrentDictionary<Type, object> vaultTypes,
    ConcurrentDictionary<Type, object> aggregateRegistries,
    ConcurrentDictionary<Type, object> projectionRegistries)
    : IMemory
{
    public IAsyncEnumerable<T> Aggregate<T>(object query) where T : notnull
        => new PostgresAggregateSource<T>(
            AggregateRegistry<T>().Sources(),
            this,
            store.QuerySession()).From(query);

    public Task<T> Projection<T>(object query) where T : notnull
        => new PostgresProjectionSource<T>(
            ProjectionRegistry<T>().Sources(),
            this,
            store.QuerySession()).From(query);

    public IVault<T> Vault<T>() where T : notnull => new PostgresVault<T>(store);

    public IBranch Branch()
        => new PostgresBranch(store.LightweightSession(), this, vaultTypes, aggregateRegistries, projectionRegistries);

    private IAggregateRegistry<T> AggregateRegistry<T>() where T : notnull
        => aggregateRegistries.TryGetValue(typeof(T), out var r)
            ? (IAggregateRegistry<T>)r
            : new PostgresAggregateRegistry<T>();

    private IProjectionRegistry<T> ProjectionRegistry<T>() where T : notnull
        => projectionRegistries.TryGetValue(typeof(T), out var r)
            ? (IProjectionRegistry<T>)r
            : new PostgresProjectionRegistry<T>();
}
