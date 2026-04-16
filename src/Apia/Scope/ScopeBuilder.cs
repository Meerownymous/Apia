namespace Apia.Scope;

/// <summary>
/// Collects <see cref="IEntitiesScope{TRecord,TFilter}"/> registrations for a specific
/// filter type <typeparamref name="TFilter"/>.
///
/// <para>
/// Build once at app startup and reuse across requests.
/// Pass to <see cref="MemoryScopeExtensions.Scoped{TFilter}"/> at runtime with the
/// concrete filter value (e.g. the current user).
/// </para>
///
/// <code>
/// // Startup:
/// var postScopes = new ScopeBuilder&lt;UserContext&gt;()
///     .Register&lt;Post&gt;(new UserPosts())
///     .Register&lt;Comment&gt;(new UserComments());
///
/// // Per-request:
/// IMemory userMemory = memory.Scoped(new UserContext(userId), postScopes);
/// </code>
/// </summary>
public sealed class ScopeBuilder<TFilter>
{
    private readonly ScopeObjectRegistry<TFilter> registry = new();

    /// <summary>
    /// Registers an <see cref="IEntitiesScope{TRecord,TFilter}"/> for
    /// <typeparamref name="TRecord"/>.
    /// </summary>
    public ScopeBuilder<TFilter> Register<TRecord>(IEntitiesScope<TRecord, TFilter> scope)
    {
        registry.Register(scope);
        return this;
    }

    internal ScopeObjectRegistry<TFilter> Build() => registry;
}
