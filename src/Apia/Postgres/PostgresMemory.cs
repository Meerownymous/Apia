using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresMemory : IMemory
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> catalogs;
    private readonly ConcurrentDictionary<Type, object> mutables;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal PostgresMemory(
        IDocumentStore store,
        ConcurrentDictionary<Type, object> catalogs,
        ConcurrentDictionary<Type, object> mutables,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.store    = store;
        this.catalogs = catalogs;
        this.mutables = mutables;
        this.sources  = sources;
    }

    public IMutableCatalog<TResult> Catalog<TResult>() where TResult : notnull
    {
        if (!catalogs.TryGetValue(typeof(TResult), out var catalog) || catalog is not PostgresMutableCatalog<TResult> pgCatalog)
            throw new InvalidOperationException($"No PostgresMutableCatalog<{typeof(TResult).Name}> registered.");
        return pgCatalog.Bind(store.LightweightSession());
    }

    public IMutable<TResult> Mutable<TResult>() where TResult : notnull
    {
        mutables.TryGetValue(typeof(TResult), out var mutable);
        return mutable is IMutable<TResult> registered
            ? registered
            : new PostgresMutable<TResult>(store.LightweightSession());
    }

    public IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, store.LightweightSession()));
    }

    public ITransaction Begin()
        => new PostgresTransaction(store.LightweightSession(), catalogs, mutables, sources);
}
