using System.Collections.Concurrent;
using Apia;

namespace Apia.File;

/// <summary>Dispatches From&lt;TQuery&gt; to registered single-result handlers.</summary>
internal sealed class FileProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> handlers,
    IMemory memory)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(TQuery query)
    {
        if (handlers.TryGetValue(typeof(TQuery), out var handler))
            return handler(query!, memory);

        throw new InvalidOperationException(
            $"No projection handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
    }
}
