using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From&lt;TQuery&gt; to registered handlers or falls back to a full table scan for AllOf&lt;T&gt;.</summary>
internal sealed class PostgresAggregateSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> handlers,
    IMemory memory,
    IDocumentSession session)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
    {
        if (query is AllOf<T>)
            return session.Query<T>().ToAsyncEnumerable();

        if (handlers.TryGetValue(typeof(TQuery), out var handler))
            return handler(query!, memory, session);

        throw new InvalidOperationException(
            $"No aggregate handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
    }
}
