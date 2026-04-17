using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>
/// Dispatches <c>From&lt;TQuery&gt;</c> to registered handlers. Always handles
/// <see cref="AllOf{T}"/> by streaming every entity in the store.
/// </summary>
public sealed class RamAggregateSource<T>(
    RamEntityStore<T> store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> handlers,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
        => query is AllOf<T>
            ? All()
            : handlers.TryGetValue(typeof(TQuery), out var handler)
                ? handler(query!, memory)
                : throw new InvalidOperationException(
                    $"No aggregate handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");

    private async IAsyncEnumerable<T> All()
    {
        foreach (var entity in store.All())
            yield return await Task.FromResult(entity);
    }
}
