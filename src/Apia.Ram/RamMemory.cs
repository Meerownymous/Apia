using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>In-memory IMemory backed by concurrent dictionaries.</summary>
public sealed class RamMemory(
    ConcurrentDictionary<Type, object> stores,
    ConcurrentDictionary<Type, object> aggregateSources,
    ConcurrentDictionary<Type, object> projectionSources)
    : IMemory
{
    public IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => aggregateSources.TryGetValue(typeof(TAggregated), out var src)
            ? ((IAggregateSource<TAggregated>)src).From<TQuery>(query)
            : throw new InvalidOperationException($"No store registered for {typeof(TAggregated).Name}.");

    public Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => projectionSources.TryGetValue(typeof(TAggregated), out var src)
            ? ((IProjectionSource<TAggregated>)src).From<TQuery>(query)
            : throw new InvalidOperationException($"No store registered for {typeof(TAggregated).Name}.");

    public IVault<T> Vault<T>()
        => stores.TryGetValue(typeof(T), out var store)
            ? new RamVault<T>((IEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new RamBranch(stores, aggregateSources, projectionSources);
}
