namespace Apia.Scope;

/// <summary>
/// Decorator that enforces registered <see cref="IAccessPolicy{TRecord,TContext}"/> on Vault
/// reads and Branch mutations. Aggregate reads pass through unchanged.
/// </summary>
public sealed class PolicyMemory<TContext>(
    IMemory inner,
    IPolicies<TContext> policies,
    TContext context)
    : IMemory
{
    public IAggregateSource<T> Aggregate<T>() => inner.Aggregate<T>();

    public IProjectionSource<T> Projection<T>() => inner.Projection<T>();

    public IVault<T> Vault<T>()
    {
        var vault = inner.Vault<T>();
        return policies.Has<T>()
            ? new PolicyEnforcedVault<T, TContext>(vault, context, policies.Of<T>())
            : vault;
    }

    public IBranch Branch()
        => new PolicyEnforcedBranch<TContext>(inner.Branch(), this, policies, context);
}
