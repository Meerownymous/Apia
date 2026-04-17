using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresProjectionRegistry<T>
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> handlers = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, Task<T>> handler)
        => handlers[typeof(TQuery)] = (q, m, s) => handler((TQuery)q, m, s);

    public ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> Handlers()
        => handlers;
}
