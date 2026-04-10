namespace Apia.Scope;

/// <summary>
/// An <see cref="ITransaction"/> decorator that keeps active policy enforcement inside a
/// transaction. The transactional <see cref="IMemory"/> exposed via <see cref="Memory"/>
/// is also a <see cref="PolicyMemory{TContext}"/> — policies are never bypassed by
/// entering a transaction.
///
/// The <see cref="IPolicies{TContext}"/> instance is shared by reference: registrations
/// always stay in sync between the outer and transactional memory.
/// </summary>
public sealed class PolicyEnforcedTransaction<TContext> : ITransaction
{
    private readonly ITransaction inner;
    private readonly IPolicies<TContext> policies;
    private readonly TContext context;

    /// <summary>Wraps <paramref name="inner"/> with the given policies and context.</summary>
    public PolicyEnforcedTransaction(
        ITransaction inner,
        IPolicies<TContext> policies,
        TContext context)
    {
        this.inner    = inner;
        this.policies = policies;
        this.context  = context;
    }

    /// <summary>Returns a policy-enforced view of the transactional memory.</summary>
    public IMemory Memory() => new PolicyMemory<TContext>(inner.Memory(), policies, context);

    /// <inheritdoc/>
    public Task Commit() => inner.Commit();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
