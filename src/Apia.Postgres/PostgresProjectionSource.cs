using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>Dispatches From&lt;TQuery&gt; to registered single-result sources.</summary>
public sealed class PostgresProjectionSource<T>(
    IReadOnlyDictionary<Type, Func<object, IMemory, IDocumentSession, Task<T>>> sources,
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
