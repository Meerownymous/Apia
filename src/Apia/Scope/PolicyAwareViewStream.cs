namespace Apia.Scope;

/// <summary>
/// Decorator for <see cref="IViewStream{TResult,TQuery}"/> that enforces access policy
/// using one of two strategies, chosen automatically at construction time:
///
/// <list type="number">
///   <item>
///     <term>Context injection (preferred)</term>
///     <description>
///       When <typeparamref name="TQuery"/> implements <see cref="IScopedQuery{TContext}"/>,
///       the active context is injected into the query before forwarding to the inner stream.
///       The backend synopsis receives the full context and can apply backend-native filtering
///       (e.g. a Postgres synopsis adds a WHERE clause; a RAM synopsis filters in-memory).
///       No post-filtering is applied here — the synopsis is responsible.
///     </description>
///   </item>
///   <item>
///     <term>Post-filter fallback</term>
///     <description>
///       When <typeparamref name="TQuery"/> does not implement <see cref="IScopedQuery{TContext}"/>
///       but a <c>canRead</c> predicate is provided, the full inner stream is iterated and
///       records that fail the predicate are dropped.  This works for all backends but does
///       not allow the backend to optimise the query.
///     </description>
///   </item>
/// </list>
/// </summary>
internal sealed class PolicyAwareViewStream<TResult, TQuery, TContext> : IViewStream<TResult, TQuery>
    where TQuery : notnull
{
    private readonly IViewStream<TResult, TQuery> inner;
    private readonly TContext context;
    private readonly Func<TResult, TContext, bool>? canRead;
    private readonly bool injectContext;

    internal PolicyAwareViewStream(
        IViewStream<TResult, TQuery> inner,
        TContext context,
        Func<TResult, TContext, bool>? canRead)
    {
        this.inner         = inner;
        this.context       = context;
        this.canRead       = canRead;
        this.injectContext = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
    }

    public async IAsyncEnumerable<TResult> From(TQuery seed)
    {
        // Strategy A: inject context into seed; synopsis handles filtering natively.
        var effectiveSeed = injectContext
            ? (TQuery)((IScopedQuery<TContext>)seed).WithContext(context)
            : seed;

        var stream = inner.From(effectiveSeed);

        // Strategy B: post-filter when the synopsis does not receive the context.
        if (!injectContext && canRead is not null)
        {
            await foreach (var result in stream)
                if (canRead(result, context))
                    yield return result;
        }
        else
        {
            await foreach (var result in stream)
                yield return result;
        }
    }
}
