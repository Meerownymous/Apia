using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public interface IPostgresAggregateSource<T, TQuery>
{
    IAsyncEnumerable<T> From(TQuery query, IMemory memory, IDocumentSession session);
}

/// <summary>Dispatches From&lt;TQuery&gt; to registered sources or falls back to a full table scan for AllOf&lt;T&gt;. Supports LinqQuery&lt;T&gt; for SQL-level scope pushdown.</summary>
public sealed class PostgresAggregateSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> sources,
    IMemory memory,
    IDocumentSession session)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
        => query is AllOf<T>
            ? session.Query<T>().ToAsyncEnumerable()
        : query is LinqQuery<T> lq
            ? session.Query<T>().Where(lq.Predicate).ToAsyncEnumerable()
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
