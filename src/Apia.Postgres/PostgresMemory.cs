using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Postgres-backed IMemory via a Marten IDocumentStore. Sessions are created per access.</summary>
public sealed class PostgresMemory(
    IDocumentStore store,
    ConcurrentDictionary<Type, object> aggregateRegistries,
    ConcurrentDictionary<Type, object> projectionRegistries)
    : IMemory
{
    public IAggregateSource<T> Aggregate<T>()
    {
        var registry = aggregateRegistries.TryGetValue(typeof(T), out var r)
            ? (PostgresAggregateRegistry<T>)r
            : new PostgresAggregateRegistry<T>();
        return new PostgresAggregateSource<T>(registry.Handlers, this, store.QuerySession());
    }

    public IProjectionSource<T> Projection<T>()
    {
        var registry = projectionRegistries.TryGetValue(typeof(T), out var r)
            ? (PostgresProjectionRegistry<T>)r
            : new PostgresProjectionRegistry<T>();
        return new PostgresProjectionSource<T>(registry.Handlers, this, store.QuerySession());
    }

    public IVault<T> Vault<T>() => new PostgresVault<T>(store);

    public IBranch Branch()
        => new PostgresBranch(store.LightweightSession(), this, aggregateRegistries, projectionRegistries);
}
