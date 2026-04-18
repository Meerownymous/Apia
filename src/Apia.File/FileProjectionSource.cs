using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>Dispatches From to registered single-result sources.</summary>
public sealed class FileProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> sources,
    IMemory memory)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(IQuery<TQuery, T> query)
        => sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query, memory)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
