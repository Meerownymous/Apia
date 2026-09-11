using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>A registry of session-aware aggregate query sources for entities of type T.</summary>
public sealed class PostgresAggregateRegistry<T> : IAggregateRegistry<T> where T : notnull
{
    private readonly ConcurrentDictionary<Type, Func<object, IMemory, IQuerySession, IAsyncEnumerable<T>>> sources = new();

    public void Register<TQuery>(Func<TQuery, IMemory, IQuerySession, IAsyncEnumerable<T>> source) where TQuery : notnull
        => sources[typeof(TQuery)] = (q, m, s) => source((TQuery)q, m, s);

    public IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, IAsyncEnumerable<T>>> Sources()
        => sources;
}
