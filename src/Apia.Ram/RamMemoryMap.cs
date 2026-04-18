using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Compose an in-memory IMemory. A type may be registered as vault, aggregate, and/or projection; only vault types are writable via IBranch.</summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> stores              = new();
    private readonly ConcurrentDictionary<Type, object> aggregateQueryMaps  = new();
    private readonly ConcurrentDictionary<Type, object> projectionQueryMaps = new();
    private readonly List<Action<IMemory, ConcurrentDictionary<Type, object>, ConcurrentDictionary<Type, object>>> buildSteps = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        var store = new RamEntityStore<T>(identity);
        stores[typeof(T)] = store;
        buildSteps.Add((memory, aggSources, _) =>
        {
            var queries = aggregateQueryMaps.TryGetValue(typeof(T), out var q)
                ? (ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>)q
                : new ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>();
            aggSources[typeof(T)] = new RamAggregateSource<T>(store, queries, memory);
        });
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        var queries = (ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>)
            aggregateQueryMaps.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>());
        var first = queries.IsEmpty;
        queries[typeof(TQuery)] = (q, m) => source.From((IQuery<TQuery>)q, m);
        if (first && !stores.ContainsKey(typeof(T)))
            buildSteps.Add((memory, aggSources, _) =>
                aggSources[typeof(T)] = new RamAggregateSource<T>(null, queries, memory));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var queries = (ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>)
            projectionQueryMaps.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>());
        var first = queries.IsEmpty;
        queries[typeof(TQuery)] = (q, m) => source.From((IQuery<TQuery>)q, m);
        if (first)
            buildSteps.Add((memory, _, projSources) =>
                projSources[typeof(T)] = new RamProjectionSource<T>(queries, memory));
    }

    public IMemory Build()
    {
        var aggSources  = new ConcurrentDictionary<Type, object>();
        var projSources = new ConcurrentDictionary<Type, object>();
        var memory = new RamMemory(stores, aggSources, projSources);
        foreach (var step in buildSteps)
            step(memory, aggSources, projSources);
        return memory;
    }
}
