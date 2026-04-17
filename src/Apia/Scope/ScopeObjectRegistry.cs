namespace Apia.Scope;

public sealed class ScopeObjectRegistry<TFilter>
{
    private readonly Dictionary<Type, object> scopes = new();

    public void Register<TRecord>(IVaultScope<TRecord, TFilter> scope)
        => scopes[typeof(TRecord)] = scope;

    public bool HasScope<TRecord>()
        => scopes.ContainsKey(typeof(TRecord));

    public IVaultScope<TRecord, TFilter> ScopeFor<TRecord>()
        => (IVaultScope<TRecord, TFilter>)scopes[typeof(TRecord)];
}
