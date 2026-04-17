namespace Apia.Scope;

public sealed class ScopeObjectRegistry<TFilter>
{
    private readonly Dictionary<Type, object> scopes = new();

    public void Register<TRecord>(IScope<TRecord, TFilter> scope)
        => scopes[typeof(TRecord)] = scope;

    public bool HasScope<TRecord>()
        => scopes.ContainsKey(typeof(TRecord));

    public IScope<TRecord, TFilter> ScopeFor<TRecord>()
        => (IScope<TRecord, TFilter>)scopes[typeof(TRecord)];
}
