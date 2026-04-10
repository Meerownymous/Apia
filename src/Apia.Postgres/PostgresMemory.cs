using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Postgres-backed IMemory via a Marten IDocumentStore. Sessions are created per query.</summary>
public sealed class PostgresMemory : IMemory
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    public PostgresMemory(
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
        if (!entities.TryGetValue(typeof(TResult), out var entry) ||
            entry is not Func<(IMemory Memory, IDocumentSession Session), IEntities<TResult>> factory)
            throw new InvalidOperationException($"No PostgresEntities<{typeof(TResult).Name}> registered.");
        return factory((this, store.LightweightSession()));
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        vaults.TryGetValue(typeof(TResult), out var vault);
        return vault is IVault<TResult> registered
            ? registered
            : new PostgresVault<TResult>(store.LightweightSession());
    }

    public OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT0(
            ((IViewStreamOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>)source)
                .From((this, store.LightweightSession())));
    }

    public OneOf.OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT0(
            ((IViewOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>)source)
                .Assemble((this, store.LightweightSession())));
    }

    public ITransaction Begin()
        => new PostgresTransaction(store.LightweightSession(), entities, vaults, sources);
}
