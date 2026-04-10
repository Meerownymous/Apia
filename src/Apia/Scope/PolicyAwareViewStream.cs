namespace Apia.Scope;

/// <summary>
/// An <see cref="IViewStream{TResult,TQuery}"/> decorator that enforces access policy
/// using one of two strategies, chosen automatically at execution time:
///
/// <list type="number">
///   <item>
///     <term>Context injection (preferred)</term>
///     <description>
///       When the seed implements <see cref="IScopedQuery{TContext}"/>, the active context
///       is injected into the seed before forwarding to the inner stream. The backend synopsis
///       receives the context-bearing seed and applies backend-native filtering.
///     </description>
///   </item>
///   <item>
///     <term>Post-filter fallback</term>
///     <description>
///       When the seed does not implement <see cref="IScopedQuery{TContext}"/>, the full
///       inner stream is iterated and records that fail <see cref="IAccessPolicy{TRecord,TContext}.CanRead"/>
///       are dropped.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class PolicyAwareViewStream<TResult, TQuery, TContext> : IViewStream<TResult, TQuery>
    where TQuery : notnull
{
    private readonly IViewStream<TResult, TQuery> inner;
    private readonly TContext context;
    private readonly IAccessPolicy<TResult, TContext> policy;

    /// <summary>Wraps <paramref name="inner"/> with the given context and access policy.</summary>
    public PolicyAwareViewStream(
        IViewStream<TResult, TQuery> inner,
        TContext context,
        IAccessPolicy<TResult, TContext> policy)
    {
        this.inner   = inner;
        this.context = context;
        this.policy  = policy;
    }

    /// <summary>
    /// Injects the context when the seed is an <see cref="IScopedQuery{TContext}"/>;
    /// otherwise post-filters results via the registered read predicate.
    /// </summary>
    public async IAsyncEnumerable<TResult> From(TQuery seed)
    {
        if (seed is IScopedQuery<TContext> scoped)
        {
            var contextualSeed = (TQuery)scoped.WithContext(context);
            await foreach (var result in inner.From(contextualSeed))
                yield return result;
        }
        else
        {
            await foreach (var result in inner.From(seed))
                if (policy.CanRead(result, context))
                    yield return result;
        }
    }
}
