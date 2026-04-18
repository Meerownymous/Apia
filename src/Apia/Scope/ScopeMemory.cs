namespace Apia.Scope;

/// <summary>
/// Decorator that enforces registered <see cref="IScope{TRecord,TFilter}"/> objects on
/// Vault access and Branch mutations. Aggregate reads are scope-filtered via
/// <see cref="ScopeFilteredAggregateSource{TRecord,TFilter}"/>.
/// Projection reads pass through unchanged — computed values cannot be post-filtered.
/// </summary>
public sealed class ScopeMemory<TFilter>(
    IMemory inner,
    IScopeRegistry<TFilter> registry,
    TFilter filter)
    : IMemory
{
    public IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
    {
        if (!registry.HasScope<TAggregated>())
            return inner.Aggregate<TAggregated, TQuery>(query);
        return new ScopeFilteredAggregateSource<TAggregated, TFilter>(inner, registry.ScopeFor<TAggregated>(), filter)
            .From<TQuery>(query);
    }

    public Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => inner.Projection<TAggregated, TQuery>(query);

    public IVault<T> Vault<T>()
    {
        var vault = inner.Vault<T>();
        return registry.HasScope<T>()
            ? new ScopeAwareVault<T, TFilter>(vault, registry.ScopeFor<T>(), filter)
            : vault;
    }

    public IBranch Branch()
        => new ScopedBranch<TFilter>(inner.Branch(), this, registry, filter);
}
