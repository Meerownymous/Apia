using Apia.Scope;

namespace Apia.Scoped;

/// <summary>
/// Wraps an <see cref="ITransaction"/> so the transactional memory returned by
/// <see cref="Memory"/> is also a <see cref="ScopeMemory{TFilter}"/> — the active scope
/// is never silently bypassed by entering a transaction.
/// </summary>
internal sealed class ScopedTransaction<TFilter> : ITransaction
{
    private readonly ITransaction inner;
    private readonly ScopeObjectRegistry<TFilter> registry;
    private readonly TFilter filter;

    internal ScopedTransaction(
        ITransaction inner,
        ScopeObjectRegistry<TFilter> registry,
        TFilter filter)
    {
        this.inner    = inner;
        this.registry = registry;
        this.filter   = filter;
    }

    public IMemory Memory() => new ScopeMemory<TFilter>(inner.Memory(), registry, filter);
    public Task Commit() => inner.Commit();
    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
