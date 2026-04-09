using Marten;

namespace Apia.Postgres;

/// <summary>
/// A Postgres-backed synopsis. The IDocumentSession is provided at query time
/// by PostgresMemory — not held at construction. Register via
/// PostgresMemoryMap.Register&lt;TShallowView, TResult, TQuery&gt;(instance).
/// </summary>
public abstract class PostgresViewStream<TResult, TQuery> : IViewStream<TResult, TQuery>
    where TQuery : Query<TResult>
{
    private readonly IMemory memory;

    /// <summary>Initialises the view with the IMemory it will query against.</summary>
    protected PostgresViewStream(IMemory memory) => this.memory = memory;

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> From(TQuery query)
        => memory.TryViewStream<TResult, TQuery>().Match(
            stream => stream.From(query),
            _ => throw new InvalidOperationException(
                $"{GetType().Name} must be registered as an override in PostgresMemoryMap."));

    /// <summary>
    /// Execute the query against a Marten session.
    /// Called by the Adapter when PostgresMemory routes here.
    /// </summary>
    protected abstract IAsyncEnumerable<TResult> From(TQuery query, IMemory memory, IDocumentSession session);
    
    /// <summary>
    /// The IViewStreamOrigin bridge PostgresMemoryMap registers on behalf of this view.
    /// Passes IMemory and IDocumentSession into Query() at execution time.
    /// </summary>
    public sealed class Adapter(PostgresViewStream<TResult, TQuery> viewStream)
        : IViewStreamOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)>
    {
        /// <inheritdoc/>
        public IViewStream<TResult, TQuery> From((IMemory Memory, IDocumentSession Session) ctx)
            => new BoundViewStream(viewStream, ctx);

        /// <summary>A view stream bound to a specific IMemory and IDocumentSession.</summary>
        private sealed class BoundViewStream(
            PostgresViewStream<TResult, TQuery> viewStream,
            (IMemory Memory, IDocumentSession Session) ctx) : IViewStream<TResult, TQuery>
        {
            /// <inheritdoc/>
            public IAsyncEnumerable<TResult> From(TQuery query) => viewStream.From(query, ctx.Memory, ctx.Session);
        }
    }
}

