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
    public IAsyncEnumerable<T> Aggregate<T>(object query)
    {
        if (!registry.HasScope<T>())
            return inner.Aggregate<T>(query);
        return new ScopeFilteredAggregateSource<T, TFilter>(
                q => inner.Aggregate<T>(q),
                registry.ScopeFor<T>(),
                filter)
            .From(query);
    }

    public Task<T> Projection<T>(object query) => inner.Projection<T>(query);

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
