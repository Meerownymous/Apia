namespace Apia.Scope;

/// <summary>
/// Wraps <see cref="IBranch"/> and enforces <see cref="IScope{TRecord,TFilter}"/> on
/// Save and Delete. Uses the outer <see cref="IMemory"/> to load entities for CanDelete checks.
/// </summary>
public sealed class ScopedBranch<TFilter>(
    IBranch inner,
    IMemory memory,
    IScopeRegistry<TFilter> registry,
    TFilter filter)
    : IBranch
{
    public IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => inner.Aggregate<TAggregated, TQuery>(query);

    public Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => inner.Projection<TAggregated, TQuery>(query);

    public Task Save<T>(T entity)
        => !registry.HasScope<T>() || registry.ScopeFor<T>().CanWrite(entity, filter)
            ? inner.Save(entity)
            : throw new UnauthorizedAccessException(
                $"Access denied: cannot save {typeof(T).Name} — CanWrite returned false.");

    public async Task Delete<T>(Guid id)
    {
        if (registry.HasScope<T>())
        {
            var result = await memory.Vault<T>().Load(id);
            result.Switch(
                record =>
                {
                    if (!registry.ScopeFor<T>().CanDelete(record, filter))
                        throw new UnauthorizedAccessException(
                            $"Access denied: cannot delete {typeof(T).Name} {id} — CanDelete returned false.");
                },
                _ => { }
            );
        }
        await inner.Delete<T>(id);
    }

    public Task Commit() => inner.Commit();
}
