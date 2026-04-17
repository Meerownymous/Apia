namespace Apia.Scope;

internal sealed class ScopeObjectRegistry<TFilter>
{
    private readonly Dictionary<Type, object> scopes = new();

    internal void Register<TRecord>(IVaultScope<TRecord, TFilter> scope)
        => scopes[typeof(TRecord)] = scope;

    internal bool HasScope<TRecord>()
        => scopes.ContainsKey(typeof(TRecord));

    internal IVaultScope<TRecord, TFilter> ScopeFor<TRecord>()
        => (IVaultScope<TRecord, TFilter>)scopes[typeof(TRecord)];
}
