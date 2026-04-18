using System.Collections.Concurrent;
using System.Linq.Expressions;
using Apia;

namespace Apia.Ram;

/// <summary>
/// Dispatches From&lt;TQuery&gt; to registered sources.
/// Vault types pass a non-null store to support IAllOf and ILinqQuery;
/// aggregate types pass null and only support registered query sources.
/// </summary>
public sealed class RamAggregateSource<T>(
    IEntityStore<T>? store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> sources,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
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
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> Filtered(Func<T, bool> predicate)
    {
        await foreach (var entity in store!.All())
            if (predicate(entity))
                yield return entity;
    }
}
