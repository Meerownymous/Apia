using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

internal sealed class PostgresAggregateRegistry<T>
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> sources = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> source)
        => sources[typeof(TQuery)] = (q, m, s) => source((TQuery)q, m, s);

    public ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> Sources()
        => sources;
}
