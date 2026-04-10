using Marten;

namespace Apia.Postgres;

/// <summary>
/// A Postgres-backed view stream origin. Subclass and implement Query()
/// to provide the Postgres-specific implementation.
/// Register via PostgresMemoryMap.Register&lt;TResult, TQuery&gt;(instance).
/// </summary>
public abstract class PostgresViewStream<TResult, TQuery>
    : IViewStreamOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>
    where TQuery : Query<TResult>
{
    /// <inheritdoc/>
    public IViewStream<TResult, TQuery> From((IMemory Memory, IDocumentSession Session) context)
        => new BoundViewStream(context, Query);

    /// <summary>Execute the query against a Marten session.</summary>
    protected abstract IAsyncEnumerable<TResult> Query(TQuery query, IMemory memory, IDocumentSession session);

    /// <summary>A view stream bound to a specific IMemory and IDocumentSession.</summary>
    private sealed class BoundViewStream(
        (IMemory Memory, IDocumentSession Session) context,
        Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TQuery>
    {
        /// <inheritdoc/>
        public IAsyncEnumerable<TResult> From(TQuery seed)
            => query(seed, context.Memory, context.Session);
    }
}
