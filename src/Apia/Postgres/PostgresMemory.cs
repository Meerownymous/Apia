using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresMemory : IMemory
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal PostgresMemory(
        IDocumentStore store,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.store    = store;
        this.entities = entities;
        this.vaults   = vaults;
        this.sources  = sources;
    }

    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not PostgresEntities<TResult> pgEntities)
            throw new InvalidOperationException($"No PostgresEntities<{typeof(TResult).Name}> registered.");
        return pgEntities.Bind(store.LightweightSession());
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        vaults.TryGetValue(typeof(TResult), out var vault);
        return vault is IVault<TResult> registered
            ? registered
            : new PostgresVault<TResult>(store.LightweightSession());
    }

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : QueryRecord<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsisStream<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, store.LightweightSession()));
    }

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : QueryRecord<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, store.LightweightSession()));   
    }

    public ITransaction Begin()
        => new PostgresTransaction(store.LightweightSession(), entities, vaults, sources);
}
