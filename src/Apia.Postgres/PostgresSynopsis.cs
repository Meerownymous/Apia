using Marten;

namespace Apia.Postgres;

/// <summary>
/// Abstract synopsis stream for Postgres-backed views.
/// Subclasses pass a query function to the base constructor.
/// IMemory and IDocumentSession are provided at query time via Grow().
/// </summary>
public abstract class PostgresSynopsisStream<TResult, TSeed>(
    Func<IMemory, IDocumentSession, TSeed, IAsyncEnumerable<TResult>> query)
    : ISynopsisStream<TResult, TSeed, (IMemory Memory, IDocumentSession Session)>
    where TSeed : notnull
{
    public IViewStream<TResult, TSeed> Grow((IMemory Memory, IDocumentSession Session) context)
        => new BoundViewStream(context, query);

    /// <summary>A view stream bound to a specific IMemory and IDocumentSession.</summary>
    private sealed class BoundViewStream(
        (IMemory Memory, IDocumentSession Session) context,
        Func<IMemory, IDocumentSession, TSeed, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TSeed>
    {
        public IAsyncEnumerable<TResult> Build(TSeed seed)
            => query(context.Memory, context.Session, seed);
    }
}
