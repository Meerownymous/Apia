using System.Collections.Concurrent;
using Marten;

namespace Apia.Postgres;

/// <summary>IMemory scoped to an active PostgresTransaction, backed by a single Marten IDocumentSession.</summary>
public sealed class PostgresTransactionMemory(
    IDocumentSession session,
    ConcurrentDictionary<Type, object> entities,
    ConcurrentDictionary<Type, object> vaults,
    ConcurrentDictionary<(Type, Type), object> sources)
    : IMemory
{
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        if (!entities.TryGetValue(typeof(TResult), out var entry) ||
            entry is not Func<IDocumentSession, IEntities<TResult>> factory)
            throw new InvalidOperationException($"No PostgresEntities<{typeof(TResult).Name}> registered.");
        return factory(session);
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        vaults.TryGetValue(typeof(TResult), out var vault);
        return vault is IVault<TResult> registered
            ? registered
            : new PostgresVault<TResult>(session);
    }

    public IViewStream<TResult, TSeed> ViewStream<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsisStream<TResult, TSeed, (IMemory Memory, IDocumentSession Session)>)source)
            .Grow((this, session));
    }

    public IView<TResult, TSeed> View<TResult, TSeed>() where TSeed : notnull
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TSeed)), out var source))
            throw new InvalidOperationException($"No ISynopsis<{typeof(TResult).Name}, {typeof(TSeed).Name}> registered.");
        return ((ISynopsis<TResult, TSeed, (IMemory Memory, IDocumentSession Session)>)source)
            .Build((this, session));
    }

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
