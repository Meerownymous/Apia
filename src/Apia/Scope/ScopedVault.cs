using System.Linq.Expressions;
using OneOf;

namespace Apia.Scope;

/// <summary>
/// Read access to the entities of type T a scope lets through. An id outside the scope reads as
/// <see cref="NotFound"/>, indistinguishable from a genuine miss. <see cref="All"/> is pushed into the
/// backend as a condition where the scope states one; <see cref="Matching"/> pushes the caller's own
/// condition and applies the scope in process.
/// </summary>
public sealed class ScopedVault<T, TFilter>(IVault<T> inner, IScope<T, TFilter> scope, TFilter filter)
    : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
        => (await inner.Entity(id)).Match<OneOf<T, NotFound>>(
            entity => scope.Includes(entity, filter) ? entity : new NotFound(),
            missing => missing);

    public IAsyncEnumerable<T> All()
        => scope.Condition(filter).Match(
            condition => inner.Matching(condition),
            _ => IncludedEntities(inner.All()));

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
        => IncludedEntities(inner.Matching(condition));

    private IAsyncEnumerable<T> IncludedEntities(IAsyncEnumerable<T> entities)
        => entities.Where(entity => scope.Includes(entity, filter));
}
