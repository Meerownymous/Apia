namespace Apia.Scope;

/// <summary>
/// Collects <see cref="IVaultScope{TRecord,TFilter}"/> registrations for a specific filter type.
/// Build once at startup; pass the result to <see cref="ScopeMemory{TFilter}"/> per-request.
/// </summary>
public sealed class ScopeBuilder<TFilter>
{
    private readonly ScopeObjectRegistry<TFilter> registry = new();

    public ScopeBuilder<TFilter> Register<TRecord>(IVaultScope<TRecord, TFilter> scope)
    {
        registry.Register(scope);
        return this;
    }

    public ScopeObjectRegistry<TFilter> Build() => registry;
}
