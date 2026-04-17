using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public interface IPostgresProjectionSource<T, TQuery>
{
    Task<T> From(TQuery query, IMemory memory, IDocumentSession session);
}

/// <summary>Dispatches From&lt;TQuery&gt; to registered single-result sources.</summary>
public sealed class PostgresProjectionSource<T>(
    ConcurrentDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> sources,
    IMemory memory,
    IDocumentSession session)
    : IProjectionSource<T>
{
    public Task<T> From<TQuery>(TQuery query)
        => sources.TryGetValue(typeof(TQuery), out var source)
            ? source(query!, memory, session)
            : throw new InvalidOperationException(
                $"No source registered for {typeof(TQuery).Name} → {typeof(T).Name}.");
}
