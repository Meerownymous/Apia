using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>File-backed unit of work. Save/Delete stage operations; Commit flushes them to disk.</summary>
internal sealed class FileBranch(
    ConcurrentDictionary<Type, object> stores,
    ConcurrentDictionary<Type, object> aggregateSources,
    ConcurrentDictionary<Type, object> projectionSources)
    : IBranch
{
    private readonly List<Func<Task>> staged = new();

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

    public async Task Commit()
    {
        foreach (var op in staged)
            await op();
        staged.Clear();
    }

    private FileEntityStore<T> Store<T>()
    {
        if (!stores.TryGetValue(typeof(T), out var store))
            throw new InvalidOperationException($"No store registered for {typeof(T).Name}.");
        return (FileEntityStore<T>)store;
    }
}
