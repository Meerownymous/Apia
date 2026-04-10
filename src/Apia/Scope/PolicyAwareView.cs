namespace Apia.Scope;

/// <summary>
/// Decorator for single-result projections (<see cref="IView{TResult,TQuery}"/>).
/// Applies the same two-strategy logic as <see cref="PolicyAwareViewStream{TResult,TQuery,TContext}"/>:
///
/// <list type="number">
///   <item>
///     <term>Context injection (preferred)</term>
///     <description>
///       When <typeparamref name="TQuery"/> implements <see cref="IScopedQuery{TContext}"/>,
///       the context is injected and the synopsis handles policy enforcement natively.
///     </description>
///   </item>
///   <item>
///     <term>Post-check fallback</term>
///     <description>
///       When <typeparamref name="TQuery"/> does not implement <see cref="IScopedQuery{TContext}"/>,
///       the result is verified against the <c>canRead</c> predicate after execution.
///       Throws <see cref="UnauthorizedAccessException"/> on denial.
///     </description>
///   </item>
/// </list>
/// </summary>
internal sealed class PolicyAwareView<TResult, TQuery, TContext> : IView<TResult, TQuery>
    where TQuery : notnull
{
    private readonly IView<TResult, TQuery> inner;
    private readonly TContext context;
    private readonly Func<TResult, TContext, bool>? canRead;
    private readonly bool injectContext;

    internal PolicyAwareView(
        IView<TResult, TQuery> inner,
        TContext context,
        Func<TResult, TContext, bool>? canRead)
    {
        this.inner         = inner;
        this.context       = context;
        this.canRead       = canRead;
        this.injectContext = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
    }

    public async Task<TResult> Query(TQuery query)
    {
        var effectiveQuery = injectContext
            ? (TQuery)((IScopedQuery<TContext>)query).WithContext(context)
            : query;

        var result = await inner.Query(effectiveQuery);

        if (!injectContext && canRead is not null && !canRead(result, context))
            throw new UnauthorizedAccessException(
                $"Access denied: view result of type {typeof(TResult).Name} is not accessible in the current context.");

        return result;
    }
}
