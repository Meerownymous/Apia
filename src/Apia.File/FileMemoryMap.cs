using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>Compose a file-backed IMemory. Each type is registered as exactly one of: vault, aggregate, or projection.</summary>
public sealed class FileMemoryMap(string directory) : IMemoryMap
{
    private readonly TypeRoleRegistry roles = new();
    private readonly ConcurrentDictionary<Type, object> stores = new();
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries  = new();
    private readonly ConcurrentDictionary<Type, object> projectionRegistries = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        roles.ClaimVault<T>();
        stores[typeof(T)] = new FileEntityStore<T>(directory, identity);
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        roles.ClaimAggregate<T>();
        var reg = (FileAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new FileAggregateRegistry<T>());
        reg.Register<TQuery>((q, m) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        roles.ClaimProjection<T>();
        var reg = (FileProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new FileProjectionRegistry<T>());
        reg.Register<TQuery>((q, m) => source.From(q, m));
    }

    public IMemory Build()
    {
        var aggregateSources  = new ConcurrentDictionary<Type, object>();
        var projectionSources = new ConcurrentDictionary<Type, object>();

        var memory = new FileMemory(stores, aggregateSources, projectionSources);

        foreach (var (type, store) in stores)
            BuildVaultForType(type, store, aggregateSources, memory);

        foreach (var (type, reg) in aggregateRegistries)
            BuildAggregateForType(type, reg, aggregateSources, memory);

        foreach (var (type, reg) in projectionRegistries)
            BuildProjectionForType(type, reg, projectionSources, memory);

        return memory;
    }

    private static void BuildVaultForType(
        Type type, object store,
        ConcurrentDictionary<Type, object> aggregateSources, IMemory memory)
    {
        typeof(FileMemoryMap)
            .GetMethod(nameof(BuildVaultTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, [store, aggregateSources, memory]);
    }

    private static void BuildVaultTyped<T>(
        FileEntityStore<T> store,
        ConcurrentDictionary<Type, object> aggregateSources, IMemory memory)
    {
        aggregateSources[typeof(T)] = new FileAggregateSource<T>(store, new(), memory);
    }

    private static void BuildAggregateForType(
        Type type, object reg,
        ConcurrentDictionary<Type, object> aggregateSources, IMemory memory)
    {
        typeof(FileMemoryMap)
            .GetMethod(nameof(BuildAggregateTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, [reg, aggregateSources, memory]);
    }

    private static void BuildAggregateTyped<T>(
        FileAggregateRegistry<T> registry,
        ConcurrentDictionary<Type, object> aggregateSources, IMemory memory)
    {
        aggregateSources[typeof(T)] = new FileAggregateSource<T>(null, registry.Handlers(), memory);
    }

    private static void BuildProjectionForType(
        Type type, object reg,
        ConcurrentDictionary<Type, object> projectionSources, IMemory memory)
    {
        typeof(FileMemoryMap)
            .GetMethod(nameof(BuildProjectionTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(type)
            .Invoke(null, [reg, projectionSources, memory]);
    }

    private static void BuildProjectionTyped<T>(
        FileProjectionRegistry<T> registry,
        ConcurrentDictionary<Type, object> projectionSources, IMemory memory)
    {
        projectionSources[typeof(T)] = new FileProjectionSource<T>(registry.Handlers(), memory);
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
