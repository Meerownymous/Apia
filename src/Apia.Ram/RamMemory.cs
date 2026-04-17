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
    {
        if (!aggregateSources.TryGetValue(typeof(T), out var src))
            throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
        return (IAggregateSource<T>)src;
    }

    public IProjectionSource<T> Projection<T>()
    {
        if (!projectionSources.TryGetValue(typeof(T), out var src))
            throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
        return (IProjectionSource<T>)src;
    }

    public IVault<T> Vault<T>()
    {
        if (!stores.TryGetValue(typeof(T), out var store))
            throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
        return new RamVault<T>((RamEntityStore<T>)store);
    }

    public IBranch Branch() => new RamBranch(stores, aggregateSources, projectionSources);
}
