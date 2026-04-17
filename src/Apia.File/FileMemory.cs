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
            ? new FileVault<T>((FileEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new FileBranch(stores, aggregateSources, projectionSources);
}
