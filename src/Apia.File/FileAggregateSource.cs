using System.Collections.Concurrent;
using System.Linq.Expressions;
using Apia;

namespace Apia.File;

/// <summary>
/// Dispatches From to registered sources.
/// Store types support AllOf and LinqQuery; aggregate-only types support registered query sources.
/// </summary>
public sealed class FileAggregateSource<T>(
    IEntityStore<T>? store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> sources,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(IQuery<TQuery, T> query)
        => query is AllOf<T>
            ? store != null
                ? store.All()
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support AllOf.")
        : query is LinqQuery<T> lq
            ? store != null
                ? Filtered(lq.Seed().Compile())
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support LinqQuery.")
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query, memory)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> Filtered(Func<T, bool> predicate)
    {
        await foreach (var entity in store!.All())
            if (predicate(entity))
                yield return entity;
    }
}
