using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Apia.DynamoDB;

/// <summary>
/// Dispatches aggregate queries to a DynamoStore or registered custom sources.
/// LinqQuery pushes the filter to DynamoDB as a server-side FilterExpression.
/// </summary>
public sealed class DynamoAggregateSource<T>(
    DynamoStore<T>? store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> sources,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From(object query)
        => query is IAllOf<T>
            ? store != null
                ? store.All()
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support AllOf.")
        : query is IQuery<Expression<Func<T, bool>>> lq
            ? store != null
                ? store.AllFiltered(lq.Seed())
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support LinqQuery.")
        : sources.TryGetValue(query.GetType(), out var source)
            ? source(query, memory)
            : throw new InvalidOperationException(
                $"No source registered for {query.GetType().Name} → {typeof(T).Name}.");
}
