using System.Collections.Concurrent;

namespace Apia.DynamoDB;

/// <summary>
/// DynamoDB unit of work. Save/Delete stage operations; Commit flushes them to DynamoDB.
/// </summary>
public sealed class DynamoBranch(
    ConcurrentDictionary<Type, object> stores,
    ConcurrentDictionary<Type, object> aggregateSources,
    ConcurrentDictionary<Type, object> projectionSources)
    : IBranch
{
    private readonly List<Func<Task>> staged = new();

    public IAsyncEnumerable<T> Aggregate<T>(object query)
        => aggregateSources.TryGetValue(typeof(T), out var src)
            ? ((IAggregateSource<T>)src).From(query)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public Task<T> Projection<T>(object query)
        => projectionSources.TryGetValue(typeof(T), out var src)
            ? ((IProjectionSource<T>)src).From(query)
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public Task Save<T>(T entity)
    {
        staged.Add(() => Store<T>().Set(entity));
        return Task.CompletedTask;
    }

    public Task Delete<T>(string id)
    {
        staged.Add(() => Store<T>().Remove(id));
        return Task.CompletedTask;
    }

    public async Task Commit()
    {
        foreach (var op in staged)
            await op();
        staged.Clear();
    }

    private IEntityStore<T> Store<T>()
        => stores.TryGetValue(typeof(T), out var store)
            ? (IEntityStore<T>)store
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
}
