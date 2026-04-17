namespace Apia.Scope;

public interface IScopeBuilder<TFilter>
{
    IScopeBuilder<TFilter> Register<TRecord>(IScope<TRecord, TFilter> scope);
    IScopeRegistry<TFilter> Build();
}
