using OneOf;

namespace Apia.Scope;

/// <summary>
/// A unit of work that refuses a save the scope does not permit and a removal the scope does not
/// permit. What it reads, it reads through a scoped memory.
/// </summary>
public sealed class ScopedBranch<TFilter>(
    IBranch inner,
    IOverrides overrides,
    IScopes<TFilter> scopes,
    TFilter filter)
    : IBranch
{
    public IMemory Memory() => new ScopeMemory<TFilter>(inner.Memory(), overrides, scopes, filter);

    public Task Save<T>(T entity) where T : notnull
        => scopes.Scope<T>().Match(
            scope => scope.CanWrite(entity, filter)
                ? inner.Save(entity)
                : throw new UnauthorizedAccessException(
                    $"Cannot save {typeof(T).Name}: the scope in force does not permit writing it."),
            _ => inner.Save(entity));

    public async Task Delete<T>(Guid id) where T : notnull
    {
        await scopes.Scope<T>().Match(
            scope => Refused(scope, id),
            _ => Task.CompletedTask);
        await inner.Delete<T>(id);
    }

    public Task<OneOf<Committed, Stale>> Commit() => inner.Commit();

    private async Task Refused<T>(IScope<T, TFilter> scope, Guid id) where T : notnull
        => (await Memory().Vault<T>().Entity(id)).Switch(
            entity =>
            {
                if (!scope.CanDelete(entity, filter))
                    throw new UnauthorizedAccessException(
                        $"Cannot delete {typeof(T).Name} {id}: the scope in force does not permit removing it.");
            },
            _ => { });
}
