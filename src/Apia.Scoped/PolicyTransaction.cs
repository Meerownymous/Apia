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
public sealed class PolicyTransaction<TContext>(
    ITransaction transaction,
    IPolicies<TContext> policies,
    TContext context)
: ITransaction
{
    /// <summary>Returns a policy-enforced view of the transactional memory.</summary>
    public IMemory Memory() => new PolicyMemory<TContext>(transaction.Memory(), policies, context);

    /// <inheritdoc/>
    public Task Commit() => transaction.Commit();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
