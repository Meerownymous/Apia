using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From to registered single-result sources.</summary>
public sealed class PostgresProjectionSource<T>(
    IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, Task<T>>> sources,
    IMemory memory,
    IQuerySession session)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(IQuery<TQuery, T> query)
        => sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
