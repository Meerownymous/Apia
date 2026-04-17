namespace Apia.Scope;

/// <summary>
/// Collects <see cref="IVaultScope{TRecord,TFilter}"/> registrations for a specific filter type.
/// Build once at startup; activate per-request via <see cref="MemoryScopeExtensions.Scoped{TFilter}"/>.
/// </summary>
public sealed class ScopeBuilder<TFilter>
{
    private readonly ScopeObjectRegistry<TFilter> registry = new();

    public ScopeBuilder<TFilter> Register<TRecord>(IVaultScope<TRecord, TFilter> scope)
    {
        registry.Register(scope);
        return this;
    }

    internal ScopeObjectRegistry<TFilter> Build() => registry;
}
