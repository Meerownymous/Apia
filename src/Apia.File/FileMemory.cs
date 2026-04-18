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
            ? new FileVault<T>((IEntityStore<T>)store)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IBranch Branch() => new FileBranch(stores, aggregateSources, projectionSources);
}
