namespace Apia.Scope;

/// <summary>
/// Decorator that enforces registered <see cref="IVaultScope{TRecord,TFilter}"/> objects on
/// Vault access and Branch mutations. Aggregate reads pass through unchanged — projections
/// handle their own filtering.
/// </summary>
internal sealed class ScopeMemory<TFilter>(
    IMemory inner,
    ScopeObjectRegistry<TFilter> registry,
    TFilter filter)
    : IMemory
{
    public IAggregateSource<T> Aggregate<T>() => inner.Aggregate<T>();

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
