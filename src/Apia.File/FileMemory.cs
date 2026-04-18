using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>File-backed IMemory. Stores records as JSON files on disk, one file per entity type.</summary>
public sealed class FileMemory(
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
            ? new FileVault<T>((IEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new FileBranch(stores, aggregateSources, projectionSources);
}
