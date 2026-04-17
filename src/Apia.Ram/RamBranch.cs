using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// In-memory unit of work. Save/Delete stage operations; Commit flushes them to the stores.
/// </summary>
internal sealed class RamBranch(
    ConcurrentDictionary<Type, object> stores,
    ConcurrentDictionary<Type, object> aggregateSources,
    ConcurrentDictionary<Type, object> projectionSources)
    : IBranch
{
    private readonly List<Action> staged = new();

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

    public Task Save<T>(T entity)
    {
        var store = Store<T>();
        staged.Add(() => store.Set(store.IdOf(entity), entity));
        return Task.CompletedTask;
    }

    public Task Delete<T>(Guid id)
    {
        var store = Store<T>();
        staged.Add(() => store.Remove(id));
        return Task.CompletedTask;
    }

    public Task Commit()
    {
        foreach (var op in staged)
            op();
        staged.Clear();
        return Task.CompletedTask;
    }

    private RamEntityStore<T> Store<T>()
    {
        if (!stores.TryGetValue(typeof(T), out var store))
            throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
        return (RamEntityStore<T>)store;
    }
}
