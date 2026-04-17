namespace Apia.Scope;

public sealed class ScopeBuilder<TFilter> : IScopeBuilder<TFilter>
{
    private readonly ScopeObjectRegistry<TFilter> registry = new();

    public IScopeBuilder<TFilter> Register<TRecord>(IScope<TRecord, TFilter> scope)
    {
        registry.Register(scope);
        return this;
    }

    public IScopeRegistry<TFilter> Build() => registry;
}
