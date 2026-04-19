using System.Collections.Concurrent;

namespace Apia.DynamoDB;

/// <summary>Dispatches single-result projection queries to registered sources.</summary>
public sealed class DynamoProjectionSource<T>(
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
