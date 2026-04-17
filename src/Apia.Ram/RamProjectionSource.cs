using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Dispatches <c>From&lt;TQuery&gt;</c> to registered single-result sources.</summary>
public sealed class RamProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> sources,
    IMemory memory)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(TQuery query)
        => sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
