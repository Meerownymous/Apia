using System.Collections.Concurrent;
using Apia;

namespace Apia.Ram;

/// <summary>Dispatches <c>From</c> to registered single-result sources.</summary>
public sealed class RamProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, Task<T>>> sources,
    IMemory memory)
    : IProjectionSource<T>
{
    public Task<T> From(object query)
        => sources.TryGetValue(query.GetType(), out var source)
            ? source(query, memory)
            : throw new InvalidOperationException(
                $"No source registered for {query.GetType().Name} → {typeof(T).Name}.");
}
