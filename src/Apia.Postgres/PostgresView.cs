using Marten;

namespace Apia.Postgres;

/// <summary>
/// A Postgres-backed view origin. Subclass and implement Query()
/// to provide the Postgres-specific implementation.
/// Register via PostgresMemoryMap.Register&lt;TShallowView, TResult, TQuery&gt;(instance).
/// </summary>
public abstract class PostgresView<TResult, TQuery>
    : IViewOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>
    where TQuery : Query<TResult>
{
    /// <inheritdoc/>
    public IView<TResult, TQuery> Assemble((IMemory Memory, IDocumentSession Session) context)
        => new BoundView(context, Query);

    /// <summary>Execute the query against a Marten session.</summary>
    protected abstract Task<TResult> Query(TQuery query, IMemory memory, IDocumentSession session);

    /// <summary>A view bound to a specific IMemory and IDocumentSession.</summary>
    private sealed class BoundView(
        (IMemory Memory, IDocumentSession Session) context,
        Func<TQuery, IMemory, IDocumentSession, Task<TResult>> query)
        : IView<TResult, TQuery>
    {
        /// <inheritdoc/>
        public Task<TResult> Query(TQuery seed)
            => query(seed, context.Memory, context.Session);
    }
}
