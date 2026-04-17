using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From&lt;TQuery&gt; to registered handlers or falls back to a full table scan for AllOf&lt;T&gt;.</summary>
public sealed class PostgresAggregateSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> handlers,
    IMemory memory,
    IDocumentSession session)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
        => query is AllOf<T>
            ? session.Query<T>().ToAsyncEnumerable()
            : handlers.TryGetValue(typeof(TQuery), out var handler)
                ? handler(query!, memory, session)
                : throw new InvalidOperationException(
                    $"No aggregate handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
