using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>
/// Dispatches From&lt;TQuery&gt; to registered sources.
/// Vault types pass a non-null store to support <see cref="AllOf{T}"/>;
/// aggregate types pass null and only support registered query sources.
/// </summary>
public sealed class FileAggregateSource<T>(
    FileEntityStore<T>? store,
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
            : sources.TryGetValue(typeof(TQuery), out var source)
                ? source(query!, memory)
                : throw new InvalidOperationException(
                    $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> All()
    {
        foreach (var entity in await store!.All())
            yield return entity;
    }
}
