using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// In-memory unit of work. Save/Delete stage operations; Commit flushes them to the stores.
/// </summary>
public sealed class RamBranch(
    ConcurrentDictionary<Type, object> stores,
    ConcurrentDictionary<Type, object> aggregateSources,
    ConcurrentDictionary<Type, object> projectionSources)
    : IBranch
{
    private readonly List<Func<Task>> staged = new();

    public IAggregateSource<T> Aggregate<T>()
        => aggregateSources.TryGetValue(typeof(T), out var src)
            ? (IAggregateSource<T>)src
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public IProjectionSource<T> Projection<T>()
        => projectionSources.TryGetValue(typeof(T), out var src)
            ? (IProjectionSource<T>)src
            : throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");

    public Task Save<T>(T entity)
    {
        staged.Add(() => Store<T>().Set(entity));
        return Task.CompletedTask;
    }

    public Task Delete<T>(Guid id)
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
