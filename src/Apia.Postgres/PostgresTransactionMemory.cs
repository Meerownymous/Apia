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
            entry is not Func<(IMemory Memory, IDocumentSession Session), IEntities<TResult>> factory)
            throw new InvalidOperationException($"No PostgresEntities<{typeof(TResult).Name}> registered.");
        return factory((this, session));
    }

    public IVault<TResult> Vault<TResult>() where TResult : notnull
    {
        vaults.TryGetValue(typeof(TResult), out var vault);
        return vault is IVault<TResult> registered
            ? registered
            : new PostgresVault<TResult>(session);
    }

    public OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT0(
            ((IViewStreamOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>)source)
                .From((this, session)));
    }

    public OneOf.OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : Query<TResult>
    {
        if (!sources.TryGetValue((typeof(TResult), typeof(TQuery)), out var source))
            return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT1(new NotFound());
        return OneOf.OneOf<IView<TResult, TQuery>, NotFound>.FromT0(
            ((IViewOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>)source)
                .Assemble((this, session)));
    }

    public ITransaction Begin()
        => throw new InvalidOperationException("Cannot begin a nested transaction.");
}
