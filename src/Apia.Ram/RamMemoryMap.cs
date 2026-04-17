using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Compose an in-memory IMemory. Each type is registered as exactly one of: vault, aggregate, or projection.</summary>
public sealed class RamMemoryMap : IMemoryMap
{
    private readonly TypeRoleRegistry roles = new();
    private readonly ConcurrentDictionary<Type, object> stores = new();
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries = new();
    private readonly ConcurrentDictionary<Type, object> projectionRegistries = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        roles.ClaimVault<T>();
        stores[typeof(T)] = new RamEntityStore<T>(identity);
        aggregateRegistries[typeof(T)] = new AggregateRegistry<T>();
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        roles.ClaimAggregate<T>();
        var registry = (AggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new AggregateRegistry<T>());
        registry.Register<TQuery>((q, m) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        roles.ClaimProjection<T>();
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

        foreach (var (type, reg) in aggregateRegistries)
            if (!stores.ContainsKey(type))
                BuildAggregateForType(type, reg, aggregateSources, memory);

        foreach (var (type, reg) in projectionRegistries)
            BuildProjectionForType(type, reg, projectionSources, memory);

        return memory;
    }

    private static void BuildForType(
        Type type,
        object store,
        ConcurrentDictionary<Type, object> aggregateSources,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        var method = typeof(RamMemoryMap)
            .GetMethod(nameof(BuildVaultTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type);
        method.Invoke(null, [store, aggregateSources, projectionSources, memory]);
    }

    private static void BuildVaultTyped<T>(
        RamEntityStore<T> store,
        ConcurrentDictionary<Type, object> aggregateSources,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        aggregateSources[typeof(T)] = new RamAggregateSource<T>(store, new(), memory);
    }

    private static void BuildAggregateForType(
        Type type,
        object reg,
        ConcurrentDictionary<Type, object> aggregateSources,
        IMemory memory)
    {
        var method = typeof(RamMemoryMap)
            .GetMethod(nameof(BuildAggregateTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type);
        method.Invoke(null, [reg, aggregateSources, memory]);
    }

    private static void BuildAggregateTyped<T>(
        AggregateRegistry<T> registry,
        ConcurrentDictionary<Type, object> aggregateSources,
        IMemory memory)
    {
        aggregateSources[typeof(T)] = new RamAggregateSource<T>(null!, registry.Handlers(), memory);
    }

    private static void BuildProjectionForType(
        Type type,
        object reg,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        var method = typeof(RamMemoryMap)
            .GetMethod(nameof(BuildProjectionTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type);
        method.Invoke(null, [reg, projectionSources, memory]);
    }

    private static void BuildProjectionTyped<T>(
        ProjectionRegistry<T> registry,
        ConcurrentDictionary<Type, object> projectionSources,
        IMemory memory)
    {
        projectionSources[typeof(T)] = new RamProjectionSource<T>(registry.Handlers(), memory);
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
