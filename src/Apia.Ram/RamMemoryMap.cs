using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Compose an in-memory IMemory. Each type is registered as exactly one of: vault, aggregate, or projection.</summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> stores           = new();
    private readonly ConcurrentDictionary<Type, object> aggregateQueryMaps  = new();
    private readonly ConcurrentDictionary<Type, object> projectionQueryMaps = new();
    private readonly List<Action<IMemory, ConcurrentDictionary<Type, object>, ConcurrentDictionary<Type, object>>> buildSteps = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        GuardRole<T>("vault");
        var store = new RamEntityStore<T>(identity);
        stores[typeof(T)] = store;
        buildSteps.Add((memory, aggSources, _) =>
            aggSources[typeof(T)] = new RamAggregateSource<T>(store, new(), memory));
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("aggregate");
        var queries = (ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>)
            aggregateQueryMaps.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>());
        var first = queries.IsEmpty;
        queries[typeof(TQuery)] = (q, m) => source.From((TQuery)q, m);
        if (first)
            buildSteps.Add((memory, aggSources, _) =>
                aggSources[typeof(T)] = new RamAggregateSource<T>(null, queries, memory));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("projection");
        var queries = (ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>)
            projectionQueryMaps.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>());
        var first = queries.IsEmpty;
        queries[typeof(TQuery)] = (q, m) => source.From((TQuery)q, m);
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

    private string? RoleOf(Type type)
        => stores.ContainsKey(type) ? "vault"
            : aggregateQueryMaps.ContainsKey(type) ? "aggregate"
            : projectionQueryMaps.ContainsKey(type) ? "projection"
            : null;

    private void GuardRole<T>(string role)
    {
        var existing = RoleOf(typeof(T));
        if (existing != null && existing != role)
            throw new InvalidOperationException(
                $"{typeof(T).Name} is already registered as {existing} and cannot also be registered as {role}.");
    }
}
