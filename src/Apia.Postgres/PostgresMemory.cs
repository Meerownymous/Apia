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
    public IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => new PostgresAggregateSource<TAggregated>(
            AggregateRegistry<TAggregated>().Sources(),
            this,
            store.QuerySession()).From<TQuery>(query);

    public Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => new PostgresProjectionSource<TAggregated>(
            ProjectionRegistry<TAggregated>().Sources(),
            this,
            store.QuerySession()).From<TQuery>(query);

    public IVault<T> Vault<T>() => new PostgresVault<T>(store);

    public IBranch Branch()
        => new PostgresBranch(store.LightweightSession(), this, vaultTypes, aggregateRegistries, projectionRegistries);

    private IAggregateRegistry<T> AggregateRegistry<T>()
        => aggregateRegistries.TryGetValue(typeof(T), out var r)
            ? (IAggregateRegistry<T>)r
            : new PostgresAggregateRegistry<T>();

    private IProjectionRegistry<T> ProjectionRegistry<T>()
        => projectionRegistries.TryGetValue(typeof(T), out var r)
            ? (IProjectionRegistry<T>)r
            : new PostgresProjectionRegistry<T>();
}
