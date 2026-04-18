using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>A registry of session-aware single-result projection sources for entities of type T.</summary>
public sealed class PostgresProjectionRegistry<T> : IProjectionRegistry<T>
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IQuerySession, Task<T>>> sources = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IQuerySession, Task<T>> source)
        => sources[typeof(TQuery)] = (q, m, s) => source((TQuery)q, m, s);

    public IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, Task<T>>> Sources()
        => sources;
}
