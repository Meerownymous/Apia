namespace Apia.Scope;

/// <summary>
/// Decorator that enforces registered <see cref="IScope{TRecord,TFilter}"/> objects on
/// Vault access and Branch mutations. Aggregate reads are scope-filtered via
/// <see cref="ScopeFilteredAggregateSource{TRecord,TFilter}"/>.
/// Projection reads pass through unchanged — computed values cannot be post-filtered.
/// </summary>
public sealed class ScopeMemory<TFilter>(
    IMemory inner,
    ScopeObjectRegistry<TFilter> registry,
    TFilter filter)
    : IMemory
{
    public IAggregateSource<T> Aggregate<T>()
    {
        var source = inner.Aggregate<T>();
        return registry.HasScope<T>()
            ? new ScopeFilteredAggregateSource<T, TFilter>(source, registry.ScopeFor<T>(), filter)
            : source;
    }

    public IProjectionSource<T> Projection<T>() => inner.Projection<T>();

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
