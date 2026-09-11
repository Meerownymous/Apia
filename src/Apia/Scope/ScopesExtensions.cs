namespace Apia.Scope;

/// <summary>Fluent composition of <see cref="IScopes{TFilter}"/> out of <see cref="ScopesWith{TEntity,TFilter}"/>.</summary>
public static class ScopesExtensions
{
    /// <summary>These scopes, extended by the rule for one further entity type.</summary>
    public static IScopes<TFilter> With<T, TFilter>(this IScopes<TFilter> scopes, IScope<T, TFilter> scope)
        where T : notnull
        => new ScopesWith<T, TFilter>(scopes, scope);
}
