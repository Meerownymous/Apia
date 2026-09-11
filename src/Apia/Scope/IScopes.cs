using OneOf;

namespace Apia.Scope;

/// <summary>The scopes a scoped memory was composed with, one per entity type.</summary>
public interface IScopes<TFilter>
{
    /// <summary>The scope given for entities of type T, or <see cref="None"/> when none was given.</summary>
    OneOf<IScope<T, TFilter>, None> Scope<T>() where T : notnull;
}
