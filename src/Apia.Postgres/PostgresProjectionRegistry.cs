using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

internal sealed class PostgresProjectionRegistry<T>
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> sources = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IDocumentSession, Task<T>> source)
        => sources[typeof(TQuery)] = (q, m, s) => source((TQuery)q, m, s);

    public ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> Sources()
        => sources;
}
