using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// Dispatches <c>From&lt;TQuery&gt;</c> to registered sources.
/// Vault types pass a non-null store to support <see cref="AllOf{T}"/> and <see cref="LinqQuery{T}"/>;
/// aggregate types pass null and only support registered query sources.
/// </summary>
public sealed class RamAggregateSource<T>(
    RamEntityStore<T>? store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> sources,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
        => query is AllOf<T>
            ? store != null
                ? All()
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support AllOf.")
        : query is LinqQuery<T> lq
            ? store != null
                ? Filtered(lq.Predicate.Compile())
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} is an aggregate type and does not support LinqQuery.")
        : sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> All()
    {
        foreach (var entity in store!.All())
            yield return await Task.FromResult(entity);
    }

    private async IAsyncEnumerable<T> Filtered(Func<T, bool> predicate)
    {
        foreach (var entity in store!.All())
            if (predicate(entity))
                yield return await Task.FromResult(entity);
    }
}
