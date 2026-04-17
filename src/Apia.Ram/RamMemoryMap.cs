using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Compose an in-memory IMemory. Register stores, query handlers, and projection handlers, then Build().</summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> stores = new();
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries = new();
    private readonly ConcurrentDictionary<Type, object> projectionRegistries = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        stores[typeof(T)] = new RamEntityStore<T>(identity);
        aggregateRegistries.TryAdd(typeof(T), new AggregateRegistry<T>());
        projectionRegistries.TryAdd(typeof(T), new ProjectionRegistry<T>());
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        var registry = (AggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new AggregateRegistry<T>());
        registry.Register<TQuery>((q, m) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var registry = (ProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new ProjectionRegistry<T>());
        registry.Register<TQuery>((q, m) => source.From(q, m));
    }

    public IMemory Build()
    {
        var aggregateSources  = new ConcurrentDictionary<Type, object>();
        var projectionSources = new ConcurrentDictionary<Type, object>();

        var memory = new RamMemory(stores, aggregateSources, projectionSources);

        foreach (var (type, store) in stores)
            BuildForType(type, store, aggregateSources, projectionSources, memory);

        return memory;
    }

    private void BuildForType(
        Type type,
        object store,
        ConcurrentDictionary<Type, object> aggregateSources,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        var method = typeof(RamMemoryMap)
            .GetMethod(nameof(BuildForTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(type);
        method.Invoke(this, [store, aggregateSources, projectionSources, memory]);
    }

    private void BuildForTyped<T>(
        RamEntityStore<T> store,
        ConcurrentDictionary<Type, object> aggregateSources,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        var aggRegistry  = aggregateRegistries.TryGetValue(typeof(T), out var ar)
            ? (AggregateRegistry<T>)ar : new AggregateRegistry<T>();
        var projRegistry = projectionRegistries.TryGetValue(typeof(T), out var pr)
            ? (ProjectionRegistry<T>)pr : new ProjectionRegistry<T>();

        aggregateSources[typeof(T)]  = new RamAggregateSource<T>(store, aggRegistry.Handlers(), memory);
        projectionSources[typeof(T)] = new RamProjectionSource<T>(projRegistry.Handlers(), memory);
    }

    private sealed class AggregateRegistry<T>
    {
        private readonly ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> handlers = new();

        public void Register<TQuery>(Func<TQuery, IMemory, IAsyncEnumerable<T>> handler)
            => handlers[typeof(TQuery)] = (q, m) => handler((TQuery)q, m);

        public ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> Handlers() => handlers;
    }

    private sealed class ProjectionRegistry<T>
    {
        private readonly ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> handlers = new();

        public void Register<TQuery>(Func<TQuery, IMemory, Task<T>> handler)
            => handlers[typeof(TQuery)] = (q, m) => handler((TQuery)q, m);

        public ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> Handlers() => handlers;
    }
}
