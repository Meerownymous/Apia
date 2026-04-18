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
    public Task<T> From(object query)
        => sources.TryGetValue(query.GetType(), out var source)
            ? source(query, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {query.GetType().Name} → {typeof(T).Name}.");
}
