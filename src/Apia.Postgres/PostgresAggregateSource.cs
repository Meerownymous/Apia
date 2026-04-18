using System.Linq.Expressions;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From&lt;TQuery&gt; to registered sources or falls back to a full table scan for IAllOf. Supports IQuery&lt;Expression&gt; for SQL-level pushdown.</summary>
public sealed class PostgresAggregateSource<T>(
    IReadOnlyDictionary<Type, Func<object, IMemory, IDocumentSession, IAsyncEnumerable<T>>> sources,
    IMemory memory,
    IDocumentSession session)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
        => query is IAllOf<T>
            ? session.Query<T>().ToAsyncEnumerable()
        : query is IQuery<Expression<Func<T, bool>>> lq
            ? session.Query<T>().Where(lq.Seed()).ToAsyncEnumerable()
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
