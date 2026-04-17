using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>Dispatches From&lt;TQuery&gt; to registered handlers or streams all entities for AllOf&lt;T&gt;.</summary>
internal sealed class FileAggregateSource<T>(
    FileEntityStore<T> store,
    ConcurrentDictionary<Type, Func<object, IMemory, IAsyncEnumerable<T>>> handlers,
    IMemory memory)
    : IAggregateSource<T>
{
    public IAsyncEnumerable<T> From<TQuery>(TQuery query)
    {
        if (query is AllOf<T>)
            return All();

        if (handlers.TryGetValue(typeof(TQuery), out var handler))
            return handler(query!, memory);

        throw new InvalidOperationException(
            $"No aggregate handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
    }

    private async IAsyncEnumerable<T> All()
    {
        foreach (var entity in await store.All())
            yield return entity;
    }
}
