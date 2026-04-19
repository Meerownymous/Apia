using System.Collections.Concurrent;
using Amazon.DynamoDBv2;

namespace Apia.DynamoDB;

/// <summary>
/// Composes a DynamoDB-backed IMemory.
/// Use RegisterStore(Func pk, Func sk) to configure PK and SK derivation per entity type.
/// IMemoryMap.RegisterStore(IIdentity) is also supported when the identity is a DynamoIdentity.
/// </summary>
public sealed class DynamoMemoryMap(IAmazonDynamoDB client, string tableName) : IMemoryMap
{
    private readonly ConcurrentDictionary<Type, object> stores              = new();
    private readonly ConcurrentDictionary<Type, object> aggregateQueryMaps  = new();
    private readonly ConcurrentDictionary<Type, object> projectionQueryMaps = new();
    private readonly List<Action<IMemory, ConcurrentDictionary<Type, object>, ConcurrentDictionary<Type, object>>> buildSteps = new();

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        if (identity is not DynamoIdentity<T> dynId)
            throw new ArgumentException(
                $"DynamoMemoryMap requires a DynamoIdentity<{typeof(T).Name}>. " +
                $"Use: new DynamoIdentity<{typeof(T).Name}>(pk: e => ..., sk: e => ...)");
        RegisterStore(dynId);
    }

    public void RegisterStore<T>(DynamoIdentity<T> identity) where T : notnull
        => RegisterStore<T>(pk: identity.Pk, sk: identity.Sk);

    public void RegisterStore<T>(Func<T, string> pk, Func<T, string> sk) where T : notnull
    {
        var store = new DynamoStore<T>(client, tableName, pk, sk);
        stores[typeof(T)] = store;
        buildSteps.Add((memory, aggSources, _) =>
        {
            var queries = aggregateQueryMaps.TryGetValue(typeof(T), out var q)
                ? (ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>)q
                : new ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>>();
            aggSources[typeof(T)] = new DynamoAggregateSource<T>(store, queries, memory);
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
                aggSources[typeof(T)] = new DynamoAggregateSource<T>(null, queries, memory));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var queries = (ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>)
            projectionQueryMaps.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>>());
        var first = queries.IsEmpty;
        queries[typeof(TQuery)] = (q, m) => source.From((IQuery<TQuery>)q, m);
        if (first)
            buildSteps.Add((memory, _, projSources) =>
                projSources[typeof(T)] = new DynamoProjectionSource<T>(queries, memory));
    }

    public IMemory Build()
    {
        var aggSources  = new ConcurrentDictionary<Type, object>();
        var projSources = new ConcurrentDictionary<Type, object>();
        var memory = new DynamoMemory(stores, aggSources, projSources);
        foreach (var step in buildSteps)
            step(memory, aggSources, projSources);
        return memory;
    }
}
