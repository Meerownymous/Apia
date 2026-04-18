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
    public IAsyncEnumerable<T> Aggregate<T>(object query)
        => aggregateSources.TryGetValue(typeof(T), out var src)
            ? ((IAggregateSource<T>)src).From(query)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public Task<T> Projection<T>(object query)
        => projectionSources.TryGetValue(typeof(T), out var src)
            ? ((IProjectionSource<T>)src).From(query)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IVault<T> Vault<T>()
        => stores.TryGetValue(typeof(T), out var store)
            ? new RamVault<T>((IEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new RamBranch(stores, aggregateSources, projectionSources);
}
