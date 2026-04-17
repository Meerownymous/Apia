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
        => new PostgresAggregateSource<T>(
            Registry<T, PostgresAggregateRegistry<T>>(aggregateRegistries).Sources(),
            this,
            store.QuerySession());

    public IProjectionSource<T> Projection<T>()
        => new PostgresProjectionSource<T>(
            Registry<T, PostgresProjectionRegistry<T>>(projectionRegistries).Sources(),
            this,
            store.QuerySession());

    public IVault<T> Vault<T>() => new PostgresVault<T>(store);

    public IBranch Branch()
        => new PostgresBranch(store.LightweightSession(), this, aggregateRegistries, projectionRegistries);

    private static TRegistry Registry<T, TRegistry>(ConcurrentDictionary<Type, object> registries)
        where TRegistry : new()
        => registries.TryGetValue(typeof(T), out var r)
            ? (TRegistry)r
            : new TRegistry();
}
