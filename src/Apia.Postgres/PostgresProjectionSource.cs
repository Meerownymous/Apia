using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From&lt;TQuery&gt; to registered single-result handlers.</summary>
internal sealed class PostgresProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> handlers,
    IMemory memory,
    IDocumentSession session)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(TQuery query)
    {
        if (handlers.TryGetValue(typeof(TQuery), out var handler))
            return handler(query!, memory, session);

        throw new InvalidOperationException(
            $"No projection handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
    }
}
