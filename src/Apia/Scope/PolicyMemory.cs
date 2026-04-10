using OneOf;

namespace Apia.Scope;

/// <summary>
/// An <see cref="IMemory"/> decorator that enforces the access policies registered in
/// <see cref="IPolicies{TContext}"/> for the given context.
///
/// <para>
/// Record types that have no registered policy pass through to the inner backend unchanged,
/// so global or shared data remains fully accessible via the same <see cref="IMemory"/>
/// reference.
/// </para>
/// </summary>
public sealed class PolicyMemory<TContext> : IMemory
{
    private readonly IMemory inner;
    private readonly IPolicies<TContext> policies;
    private readonly TContext context;

    /// <summary>Wraps <paramref name="inner"/> with the given policies and context.</summary>
    public PolicyMemory(IMemory inner, IPolicies<TContext> policies, TContext context)
    {
        this.inner    = inner;
        this.policies = policies;
        this.context  = context;
    }

    /// <summary>
    /// Returns a policy-enforced wrapper when a policy is registered for
    /// <typeparamref name="TResult"/>; otherwise delegates directly to the backend.
    /// </summary>
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        var entities = inner.Entities<TResult>();
        return policies.Has<TResult>()
            ? new PolicyEnforcedEntities<TResult, TContext>(entities, context, policies.Of<TResult>())
            : entities;
    }

    /// <summary>
    /// Vaults are semantically singletons and are passed through unchanged.
    /// For per-context singleton data, model it as <see cref="IEntities{TRecord}"/>
    /// keyed by a context-derived identifier instead.
    /// </summary>
    public IVault<TResult> Vault<TResult>() where TResult : notnull
        => inner.Vault<TResult>();

    /// <summary>
    /// Returns a <see cref="PolicyAwareViewStream{TResult,TQuery,TContext}"/> when the query
    /// supports context injection or a read predicate is registered; otherwise delegates directly.
    /// Returns <see cref="NotFound"/> when no stream is registered in the inner backend.
    /// </summary>
    public OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : notnull, Query<TResult>
    {
        return inner.TryViewStream<TResult, TQuery>().Match<OneOf<IViewStream<TResult, TQuery>, NotFound>>(
            stream =>
            {
                var isScoped   = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
                var hasPolicy  = policies.Has<TResult>();

                if (!isScoped && !hasPolicy)
                    return OneOf<IViewStream<TResult, TQuery>, NotFound>.FromT0(stream);

                var policy = hasPolicy
                    ? policies.Of<TResult>()
                    : (IAccessPolicy<TResult, TContext>)new OpenAccessPolicy<TResult, TContext>();

                return new PolicyAwareViewStream<TResult, TQuery, TContext>(stream, context, policy);
            },
            notFound => notFound
        );
    }

    /// <summary>
    /// Returns a <see cref="PolicyAwareView{TResult,TQuery,TContext}"/> when the query
    /// supports context injection or a read predicate is registered; otherwise delegates directly.
    /// Returns <see cref="NotFound"/> when no view is registered in the inner backend.
    /// </summary>
    public OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : notnull, Query<TResult>
    {
        return inner.TryView<TResult, TQuery>().Match<OneOf<IView<TResult, TQuery>, NotFound>>(
            view =>
            {
                var isScoped  = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
                var hasPolicy = policies.Has<TResult>();

                if (!isScoped && !hasPolicy)
                    return OneOf<IView<TResult, TQuery>, NotFound>.FromT0(view);

                var policy = hasPolicy
                    ? policies.Of<TResult>()
                    : (IAccessPolicy<TResult, TContext>)new OpenAccessPolicy<TResult, TContext>();

                return new PolicyAwareView<TResult, TQuery, TContext>(view, context, policy);
            },
            notFound => notFound
        );
    }

    /// <summary>
    /// Begins a transaction whose <see cref="ITransaction.Memory"/> is also a
    /// <see cref="PolicyMemory{TContext}"/> — policies are never bypassed inside transactions.
    /// </summary>
    public ITransaction Begin()
        => new PolicyEnforcedTransaction<TContext>(inner.Begin(), policies, context);
}
