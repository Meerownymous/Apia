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
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.session        = session;
        transactionalMemory = new TransactionalMemory(session, entities, vaults, sources);
    }

    public IMemory Memory() => transactionalMemory;

    public async Task Commit() => await session.SaveChangesAsync();

    public async ValueTask DisposeAsync() => await session.DisposeAsync();
}

internal sealed class TransactionalMemory : IMemory
{
    private readonly IDocumentSession session;
    private readonly ConcurrentDictionary<Type, object> entities;
    private readonly ConcurrentDictionary<Type, object> vaults;
    private readonly ConcurrentDictionary<(Type, Type), object> sources;

    internal TransactionalMemory(
        IDocumentSession session,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.session  = session;
        this.entities = entities;
        this.vaults   = vaults;
        this.sources  = sources;
    }

    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) || entry is not PostgresEntities<TResult> pgEntities)
            throw new InvalidOperationException($"No PostgresEntities<{typeof(TResult).Name}> registered.");
        return pgEntities.Bind(session);
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        vaults.TryGetValue(typeof(TResult), out var vault);
        return vault is IVault<TResult> registered
            ? registered
            : new PostgresVault<TResult>(session);
    }

    public IViewStream<TResult, TQuery> Views<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsisStream<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, session));
    }

    public IView<TResult, TQuery> View<TResult, TQuery>() where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TQuery).Name}> registered.");
        return ((ISynopsis<TResult, TQuery, (IMemory, IDocumentSession)>)source)
            .Build((this, session));
    }
        

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
