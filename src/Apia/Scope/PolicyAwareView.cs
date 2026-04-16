namespace Apia.Scope;

/// <summary>
/// An <see cref="IView{TResult,TQuery}"/> decorator that enforces access policy
/// using one of two strategies, chosen automatically at execution time:
///
/// <list type="number">
///   <item>
///     <term>Context injection (preferred)</term>
///     <description>
///       When the query implements <see cref="IScopedQuery{TContext}"/>, the active context
///       is injected before forwarding to the inner view. The synopsis handles filtering natively.
///     </description>
///   </item>
///   <item>
///     <term>Post-check fallback</term>
///     <description>
///       When the query does not implement <see cref="IScopedQuery{TContext}"/>, the result
///       is verified against <see cref="IAccessPolicy{TRecord,TContext}.CanRead"/> after execution.
///       Throws <see cref="UnauthorizedAccessException"/> on denial.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class PolicyAwareView<TResult, TQuery, TContext> : IView<TResult, TQuery>
    where TQuery : notnull
{
    private readonly IView<TResult, TQuery> inner;
    private readonly TContext context;
    private readonly IAccessPolicy<TResult, TContext> policy;

    /// <summary>Wraps <paramref name="inner"/> with the given context and access policy.</summary>
    public PolicyAwareView(
        IView<TResult, TQuery> inner,
        TContext context,
        IAccessPolicy<TResult, TContext> policy)
    {
        this.inner   = inner;
        this.context = context;
        this.policy  = policy;
    }

    /// <summary>
    /// Injects the context when the query is an <see cref="IScopedQuery{TContext}"/>;
    /// otherwise executes and post-checks the result via the registered read predicate.
    /// </summary>
    public async Task<TResult> Query(TQuery query)
    {
        if (query is IScopedQuery<TContext> scoped)
        {
            var contextualQuery = (TQuery)scoped.WithContext(context);
            return await inner.Query(contextualQuery);
        }

        var result = await inner.Query(query);

        if (!policy.CanRead(result, context))
            throw new UnauthorizedAccessException(
                $"Access denied: view result of type {typeof(TResult).Name} is not accessible in the current context.");

        return result;
    }
}
