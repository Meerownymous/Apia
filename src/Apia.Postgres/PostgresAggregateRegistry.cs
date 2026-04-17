using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

internal sealed class PostgresAggregateRegistry<T>
{
    internal readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> Handlers = new();

    internal void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> handler)
        => Handlers[typeof(TQuery)] = (q, m, s) => handler((TQuery)q, m, s);
}
