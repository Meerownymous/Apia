using System.Linq.Expressions;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From to registered sources or falls back to a full table scan for IAllOf. Supports IQuery&lt;Expression&gt; for SQL-level pushdown.</summary>
public sealed class PostgresAggregateSource<T>(
    IReadOnlyDictionary<Type, Func<object, IMemory, IQuerySession, IAsyncEnumerable<T>>> sources,
    IMemory memory,
    IQuerySession session)
    : IAggregateSource<T> where T : notnull
{
    public IAsyncEnumerable<T> From(object query)
        => query is IAllOf<T>
            ? session.Query<T>().ToAsyncEnumerable()
        : query is IQuery<Expression<Func<T, bool>>> lq
            ? session.Query<T>().Where(lq.Seed()).ToAsyncEnumerable()
        : sources.TryGetValue(query.GetType(), out var source)
            ? source(query, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {query.GetType().Name} → {typeof(T).Name}.");
}
