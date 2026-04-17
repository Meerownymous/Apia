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
    public IAggregateSource<T> Aggregate<T>()
        => aggregateSources.TryGetValue(typeof(T), out var src)
            ? (IAggregateSource<T>)src
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IProjectionSource<T> Projection<T>()
        => projectionSources.TryGetValue(typeof(T), out var src)
            ? (IProjectionSource<T>)src
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IVault<T> Vault<T>()
        => stores.TryGetValue(typeof(T), out var store)
            ? new RamVault<T>((IEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new RamBranch(stores, aggregateSources, projectionSources);
}
