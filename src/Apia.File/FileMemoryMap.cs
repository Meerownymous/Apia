using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>Compose a file-backed IMemory. Register stores and handlers, then call Build().</summary>
public sealed class FileMemoryMap(string directory) : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> stores = new();
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries  = new();
    private readonly ConcurrentDictionary<Type, object> projectionRegistries = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        stores[typeof(T)] = new FileEntityStore<T>(directory, identity);
        aggregateRegistries.TryAdd(typeof(T), new FileAggregateRegistry<T>());
        projectionRegistries.TryAdd(typeof(T), new FileProjectionRegistry<T>());
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        var reg = (FileAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new FileAggregateRegistry<T>());
        reg.Register<TQuery>((q, m) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var reg = (FileProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new FileProjectionRegistry<T>());
        reg.Register<TQuery>((q, m) => source.From(q, m));
    }

    public IMemory Build()
    {
        var aggregateSources  = new ConcurrentDictionary<Type, object>();
        var projectionSources = new ConcurrentDictionary<Type, object>();

        var memory = new FileMemory(stores, aggregateSources, projectionSources);

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
        var method = typeof(FileMemoryMap)
            .GetMethod(nameof(BuildForTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(type);
        method.Invoke(this, [store, aggregateSources, projectionSources, memory]);
    }

    private void BuildForTyped<T>(
        FileEntityStore<T> store,
        ConcurrentDictionary<Type, object> aggregateSources,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        var aggReg  = aggregateRegistries.TryGetValue(typeof(T), out var ar)
            ? (FileAggregateRegistry<T>)ar : new FileAggregateRegistry<T>();
        var projReg = projectionRegistries.TryGetValue(typeof(T), out var pr)
            ? (FileProjectionRegistry<T>)pr : new FileProjectionRegistry<T>();

        aggregateSources[typeof(T)]  = new FileAggregateSource<T>(store, aggReg.Handlers(), memory);
        projectionSources[typeof(T)] = new FileProjectionSource<T>(projReg.Handlers(), memory);
    }

    private sealed class FileAggregateRegistry<T>
    {
        private readonly ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> handlers = new();

        public void Register<TQuery>(Func<TQuery, IMemory, IAsyncEnumerable<T>> h)
            => handlers[typeof(TQuery)] = (q, m) => h((TQuery)q, m);

        public ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> Handlers() => handlers;
    }

    private sealed class FileProjectionRegistry<T>
    {
        private readonly ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> handlers = new();

        public void Register<TQuery>(Func<TQuery, IMemory, Task<T>> h)
            => handlers[typeof(TQuery)] = (q, m) => h((TQuery)q, m);

        public ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> Handlers() => handlers;
    }
}
