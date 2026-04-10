namespace Apia.Scope;

/// <summary>
/// Transparent wrapper that keeps the active policy enforcement inside a transaction.
///
/// <see cref="PolicyMemory{TContext}.Begin"/> returns this so that the transactional
/// <see cref="IMemory"/> exposed via <see cref="Memory"/> is also a
/// <see cref="PolicyMemory{TContext}"/> — policies are never silently bypassed by
/// entering a transaction.
///
/// The <see cref="PolicyRegistry{TContext}"/> is shared by reference — registrations
/// always stay in sync between the outer and transactional memory.
///
/// Commit / rollback semantics belong entirely to the inner transaction.
/// </summary>
internal sealed class PolicyEnforcedTransaction<TContext> : ITransaction
{
    private readonly ITransaction inner;
    private readonly PolicyRegistry<TContext> registry;
    private readonly TContext context;

    internal PolicyEnforcedTransaction(
        ITransaction inner,
        PolicyRegistry<TContext> registry,
        TContext context)
    {
        this.inner    = inner;
        this.registry = registry;
        this.context  = context;
    }

    /// <summary>Returns a policy-enforced view of the transactional memory.</summary>
    public IMemory Memory() => new PolicyMemory<TContext>(inner.Memory(), registry, context);

    public Task Commit() => inner.Commit();

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
