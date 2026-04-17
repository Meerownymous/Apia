using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

internal sealed class PostgresProjectionRegistry<T>
{
    internal readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> Handlers = new();

    internal void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, Task<T>> handler)
        => Handlers[typeof(TQuery)] = (q, m, s) => handler((TQuery)q, m, s);
}
