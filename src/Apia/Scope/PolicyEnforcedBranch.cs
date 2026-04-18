namespace Apia.Scope;

/// <summary>
/// Wraps <see cref="IBranch"/> and enforces <see cref="IAccessPolicy{TRecord,TContext}"/> on
/// Save and Delete. Uses the outer <see cref="IMemory"/> to load entities for CanDelete checks.
/// </summary>
public sealed class PolicyEnforcedBranch<TContext>(
    IBranch inner,
    IMemory memory,
    IPolicies<TContext> policies,
    TContext context)
    : IBranch
{
    public IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => inner.Aggregate<TAggregated, TQuery>(query);

    public Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query)
        => inner.Projection<TAggregated, TQuery>(query);

    public Task Save<T>(T entity)
    {
        if (policies.Has<T>() && !policies.Of<T>().CanWrite(entity, context))
            throw new UnauthorizedAccessException(
                $"Access denied: cannot save {typeof(T).Name} in the current context.");
        return inner.Save(entity);
    }

    public async Task Delete<T>(Guid id)
    {
        if (policies.Has<T>())
        {
            var result = await memory.Vault<T>().Load(id);
            result.Switch(
                record =>
                {
                    if (!policies.Of<T>().CanDelete(record, context))
                        throw new UnauthorizedAccessException(
                            $"Access denied: cannot delete {typeof(T).Name} {id} in the current context.");
                },
                _ => { }
            );
        }
        await inner.Delete<T>(id);
    }

    public Task Commit() => inner.Commit();
}
