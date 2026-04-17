namespace Apia.Scope;

public interface IScopeRegistry<TFilter>
{
    bool HasScope<TRecord>();
    IScope<TRecord, TFilter> ScopeFor<TRecord>();
}
