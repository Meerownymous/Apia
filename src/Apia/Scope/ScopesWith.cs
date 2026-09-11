using OneOf;

namespace Apia.Scope;

/// <summary>Scopes extended by the rule for one further entity type.</summary>
public sealed class ScopesWith<TEntity, TFilter>(IScopes<TFilter> inner, IScope<TEntity, TFilter> scope)
    : IScopes<TFilter> where TEntity : notnull
{
    public OneOf<IScope<T, TFilter>, None> Scope<T>() where T : notnull
        => scope is IScope<T, TFilter> given
            ? OneOf<IScope<T, TFilter>, None>.FromT0(given)
            : inner.Scope<T>();
}
