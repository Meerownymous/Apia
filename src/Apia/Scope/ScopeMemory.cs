using OneOf;

namespace Apia.Scope;

/// <summary>
/// Decorator that applies registered <see cref="IEntitiesScope{TRecord,TFilter}"/> objects
/// to entity access based on an active filter value of type <typeparamref name="TFilter"/>.
///
/// <para>
/// Created by <see cref="MemoryScopeExtensions.Scoped{TFilter}"/>.
/// Do not instantiate directly.
/// </para>
///
/// <para>
/// Record types with no registered scope pass through to the inner backend unchanged —
/// global / shared data remains fully accessible.
/// </para>
/// </summary>
internal sealed class ScopeMemory<TFilter> : IMemory
{
    private readonly IMemory inner;
    private readonly ScopeObjectRegistry<TFilter> registry;
    private readonly TFilter filter;

    internal ScopeMemory(IMemory inner, ScopeObjectRegistry<TFilter> registry, TFilter filter)
    {
        this.inner    = inner;
        this.registry = registry;
        this.filter   = filter;
    }

    /// <summary>
    /// Wraps the inner entities with <see cref="ScopeAwareEntities{TRecord,TFilter}"/>
    /// when a scope is registered for <typeparamref name="TResult"/>.
    /// Otherwise delegates directly to the backend.
    /// </summary>
    public IEntities<TResult> Entities<TResult>() where TResult : notnull
    {
        var entities = inner.Entities<TResult>();
        return registry.HasScope<TResult>()
            ? new ScopeAwareEntities<TResult, TFilter>(entities, registry.ScopeFor<TResult>(), filter)
            : entities;
    }

    /// <summary>Vaults are global — passed through unchanged.</summary>
    public IVault<TResult> Vault<TResult>() where TResult : notnull
        => inner.Vault<TResult>();

    public OneOf<IViewStream<TResult, TQuery>, NotFound> TryViewStream<TResult, TQuery>()
        where TQuery : notnull
        => inner.TryViewStream<TResult, TQuery>();

    public OneOf<IView<TResult, TQuery>, NotFound> TryView<TResult, TQuery>()
        where TQuery : notnull
        => inner.TryView<TResult, TQuery>();

    /// <summary>
    /// Begins a transaction whose memory also enforces the active scope.
    /// </summary>
    public ITransaction Begin()
        => new ScopedTransaction<TFilter>(inner.Begin(), registry, filter);
}
