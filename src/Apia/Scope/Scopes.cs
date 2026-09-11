using OneOf;

namespace Apia.Scope;

/// <summary>The scopes of no entity type at all. The root a composition extends with <c>With</c>.</summary>
public sealed class Scopes<TFilter> : IScopes<TFilter>
{
    public OneOf<IScope<T, TFilter>, None> Scope<T>() where T : notnull => new None();
}
