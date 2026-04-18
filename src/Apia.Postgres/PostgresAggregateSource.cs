using System.Linq.Expressions;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From to registered sources or falls back to a full table scan for AllOf. Supports LinqQuery for SQL-level pushdown.</summary>
public sealed class PostgresAggregateSource<T>(
    IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, IAsyncEnumerable<T>>> sources,
    IMemory memory,
    IQuerySession session)
    : IAggregateSource<T> where T : notnull
{
    public IAsyncEnumerable<T> From<TQuery>(IQuery<TQuery, T> query)
        => query is AllOf<T>
            ? session.Query<T>().ToAsyncEnumerable()
        : query is LinqQuery<T> lq
            ? session.Query<T>().Where(lq.Seed()).ToAsyncEnumerable()
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
