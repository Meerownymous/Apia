using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public interface IPostgresProjectionSource<T, TQuery>
{
    Task<T> From(TQuery query, IMemory memory, IDocumentSession session);
}

/// <summary>Dispatches From&lt;TQuery&gt; to registered single-result handlers.</summary>
public sealed class PostgresProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> handlers,
    IMemory memory,
    IDocumentSession session)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(TQuery query)
        => handlers.TryGetValue(typeof(TQuery), out var handler)
            ? handler(query!, memory, session)
            : throw new InvalidOperationException(
                $"No projection handler registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
