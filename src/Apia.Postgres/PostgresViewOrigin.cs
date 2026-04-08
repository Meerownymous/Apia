using Marten;

namespace Apia.Postgres;

/// <summary>
/// Abstract synopsis stream for Postgres-backed views.
/// Subclasses pass a query function to the base constructor.
/// IMemory and IDocumentSession are provided at query time via Grow().
/// </summary>
public abstract class PostgresViewOrigin<TResult, TSeed>(
    Func<IMemory, IDocumentSession, TSeed, Task<TResult>> query)
    : IViewOrigin<TResult, TSeed, (IMemory Memory, IDocumentSession Session)>
    where TSeed : notnull
{
    public IView<TResult, TSeed> Assemble((IMemory Memory, IDocumentSession Session) context)
        => new BoundView(context, query);

    /// <summary>A view stream bound to a specific IMemory and IDocumentSession.</summary>
    private sealed class BoundView(
        (IMemory Memory, IDocumentSession Session) context,
        Func<IMemory, IDocumentSession, TSeed, Task<TResult>> query)
        : IView<TResult, TSeed>
    {
        public Task<TResult> Query(TSeed seed)
            => query(context.Memory, context.Session, seed);
    }
}