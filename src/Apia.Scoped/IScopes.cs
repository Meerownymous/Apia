namespace Apia.Scoped;

/// <summary>
/// A typed collection of <see cref="IEntitiesScope{TRecord,TFilter}"/> registrations
/// for a specific filter type <typeparamref name="TFilter"/>.
///
/// <para>
/// Build once at application startup and pass per request with a concrete filter value
/// to <see cref="MemoryScopeExtensions.Scoped{TFilter}"/>.
/// </para>
/// </summary>
public interface IScopes<TFilter>
{
    /// <summary>
    /// Registers an <see cref="IEntitiesScope{TRecord,TFilter}"/> for
    /// <typeparamref name="TRecord"/> and returns <c>this</c> (fluent).
    /// </summary>
    IScopes<TFilter> Register<TRecord>(IEntitiesScope<TRecord, TFilter> scope)
        where TRecord : notnull;

    /// <summary>Returns <see langword="true"/> if a scope is registered for <typeparamref name="TRecord"/>.</summary>
    bool Has<TRecord>() where TRecord : notnull;

    /// <summary>Returns the registered scope for <typeparamref name="TRecord"/>.</summary>
    IEntitiesScope<TRecord, TFilter> For<TRecord>() where TRecord : notnull;
}
