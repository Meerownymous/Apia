using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresTransaction : ITransaction
{
    private readonly IDocumentSession session;
    private readonly TransactionalMemory transactionalMemory;

    internal PostgresTransaction(
        IDocumentSession session,
        ConcurrentDictionary<Type, object> catalogs,
        ConcurrentDictionary<Type, object> mutables,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.session        = session;
        transactionalMemory = new TransactionalMemory(session, catalogs, mutables, sources);
    }

    public IMemory Memory() => transactionalMemory;

    public async Task Commit() => await session.SaveChangesAsync();

    public async ValueTask DisposeAsync() => await session.DisposeAsync();
}

internal sealed class TransactionalMemory : IMemory
{
    private readonly IDocumentSession session;
    private readonly ConcurrentDictionary<Type, object> catalogs;
    private readonly ConcurrentDictionary<Type, object> mutables;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal TransactionalMemory(
        IDocumentSession session,
        ConcurrentDictionary<Type, object> catalogs,
        ConcurrentDictionary<Type, object> mutables,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.session  = session;
        this.catalogs = catalogs;
        this.mutables = mutables;
        this.sources  = sources;
    }

    public IMutableCatalog<TResult> Catalog<TResult>() where TResult : notnull
    {
        if (!catalogs.TryGetValue(typeof(TResult), out var catalog) || catalog is not PostgresMutableCatalog<TResult> pgCatalog)
            throw new InvalidOperationException($"No PostgresMutableCatalog<{typeof(TResult).Name}> registered.");
        return pgCatalog.Bind(session);
    }

    public IMutable<TResult> Mutable<TResult>() where TResult : notnull
    {
        mutables.TryGetValue(typeof(TResult), out var mutable);
        return mutable is IMutable<TResult> registered
            ? registered
            : new PostgresMutable<TResult>(session);
    }

    public IProjection<TResult, TQuery> Synopsis<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, session));
    }

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
