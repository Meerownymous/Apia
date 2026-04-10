using OneOf;

namespace Apia.Scope;

/// <summary>
/// The main decorator. Wraps any <see cref="IMemory"/> (RAM, File, Postgres, …) and
/// enforces the access policies registered in <see cref="PolicyRegistry{TContext}"/>.
///
/// <para>
/// Record types that have no registered policy pass through to the inner backend unchanged,
/// so global / shared data remains fully accessible via the same <see cref="IMemory"/>
/// reference.
/// </para>
///
/// <para>
/// Do not instantiate directly — use
/// <see cref="MemoryPolicyExtensions.WithPolicy{TContext}"/> instead.
/// </para>
/// </summary>
public sealed class PolicyMemory<TContext> : IMemory
{
    private readonly IMemory inner;
    private readonly PolicyRegistry<TContext> registry;
    private readonly TContext context;

    internal PolicyMemory(IMemory inner, PolicyRegistry<TContext> registry, TContext context)
    {
        this.inner    = inner;
        this.registry = registry;
        this.context  = context;
    }

    /// <summary>
    /// Returns a policy-enforced wrapper when a policy is registered for
    /// <typeparamref name="TResult"/>; otherwise delegates directly to the backend.
    /// </summary>
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        var entities = inner.Entities<TResult>();
        return registry.HasPolicy<TResult>()
            ? new PolicyEnforcedEntities<TResult, TContext>(entities, context, registry.PolicyFor<TResult>())
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
    /// Returns a <see cref="PolicyAwareViewStream{TResult,TQuery,TContext}"/> when
    /// the query supports context injection or a read predicate is registered.
    /// Strategy selection (injection vs. post-filter) is determined at construction.
    /// Returns <see cref="NotFound"/> when no stream is registered in the inner backend.
    /// </summary>
    public OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : notnull
    {
        return inner.TryViewStream<TResult, TQuery>().Match<OneOf<IViewStream<TResult, TQuery>, NotFound>>(
            stream =>
            {
                var injectContext = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
                var hasReadPolicy = registry.HasPolicy<TResult>();

                if (!injectContext && !hasReadPolicy)
                    return stream;

                var canRead = hasReadPolicy ? registry.PolicyFor<TResult>().CanRead : null;
                return new PolicyAwareViewStream<TResult, TQuery, TContext>(stream, context, canRead);
            },
            notFound => notFound
        );
    }

    /// <summary>
    /// Returns a <see cref="PolicyAwareView{TResult,TQuery,TContext}"/> when
    /// the query supports context injection or a read predicate is registered.
    /// Returns <see cref="NotFound"/> when no view is registered in the inner backend.
    /// </summary>
    public OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : notnull
    {
        return inner.TryView<TResult, TQuery>().Match<OneOf<IView<TResult, TQuery>, NotFound>>(
            view =>
            {
                var injectContext = typeof(IScopedQuery<TContext>).IsAssignableFrom(typeof(TQuery));
                var hasReadPolicy = registry.HasPolicy<TResult>();

                if (!injectContext && !hasReadPolicy)
                    return view;

                var canRead = hasReadPolicy ? registry.PolicyFor<TResult>().CanRead : null;
                return new PolicyAwareView<TResult, TQuery, TContext>(view, context, canRead);
            },
            notFound => notFound
        );
    }

    /// <summary>
    /// Begins a transaction whose <see cref="ITransaction.Memory"/> is also
    /// a <see cref="PolicyMemory{TContext}"/> — policies are never bypassed inside transactions.
    /// </summary>
    public ITransaction Begin()
        => new PolicyEnforcedTransaction<TContext>(inner.Begin(), registry, context);
}
