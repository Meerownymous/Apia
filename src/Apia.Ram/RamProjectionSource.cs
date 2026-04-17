using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Dispatches <c>From&lt;TQuery&gt;</c> to registered single-result handlers.</summary>
internal sealed class RamProjectionSource<T>(
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
