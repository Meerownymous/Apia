using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresAggregateRegistry<T>
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> handlers = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> handler)
        => handlers[typeof(TQuery)] = (q, m, s) => handler((TQuery)q, m, s);

    public ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> Handlers()
        => handlers;
}
