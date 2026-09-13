namespace Apia.Scope;

/// <summary>
/// A memory that reads and writes only what the scopes in force permit for one filter value. A query's
/// own implementation runs against this memory, so the scope holds inside it. A backend override does
/// not read through the vault and therefore reads past the scope — see
/// docs/adr/0001-backend-overrides-bypass-scopes.md. This is not a complete boundary.
/// <para>
/// The overrides answering here are the ones handed to this memory, not the ones the wrapped memory was
/// composed with, which it does not publish. Which overrides read past this scope is therefore a
/// decision the composition states: the same collection to keep them, <see cref="Overrides"/> to let
/// every query answer itself inside the scope.
/// </para>
/// </summary>
public sealed class ScopeMemory<TFilter>(
    IMemory inner,
    IOverrides overrides,
    IScopes<TFilter> scopes,
    TFilter filter)
    : IMemory
{
    public IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query) where T : notnull
        => overrides.Results(query, this).Match(results => results, _ => query.Results(this));

    public Task<T> Projection<T>(IProjectionQuery<T> query) where T : notnull
        => overrides.Result(query, this).Match(result => result, _ => query.Result(this));

    public IVault<T> Vault<T>() where T : notnull
        => scopes.Scope<T>().Match<IVault<T>>(
            scope => new ScopedVault<T, TFilter>(inner.Vault<T>(), scope, filter),
            _ => inner.Vault<T>());

    public IBranch Branch() => new ScopedBranch<TFilter>(inner.Branch(), overrides, scopes, filter);
}
