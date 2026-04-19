using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Apia.DynamoDB;

/// <summary>
/// Dispatches aggregate queries to a DynamoStore or registered custom sources.
/// AllOf and LinqQuery are handled via store.All() with optional in-process filtering.
/// </summary>
public sealed class DynamoAggregateSource<T>(
    IEntityStore<T>? store,
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
                ? Filtered(lq.Seed().Compile())
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support LinqQuery.")
        : sources.TryGetValue(query.GetType(), out var source)
            ? source(query, memory)
            : throw new InvalidOperationException(
                $"No source registered for {query.GetType().Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> Filtered(Func<T, bool> predicate)
    {
        await foreach (var entity in store!.All())
            if (predicate(entity))
                yield return entity;
    }
}
