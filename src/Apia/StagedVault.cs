using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>
/// Read access to the entities of type T as a branch sees them: what it staged, layered over what the
/// store holds. Reading by id notes the version read, which is what a commit compares against.
/// Reading through <see cref="All"/> or <see cref="Matching"/> notes nothing, so streaming a type
/// does not make every entity of it a candidate for a stale commit.
/// </summary>
public sealed class StagedVault<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
    {
        if (new LatestSaved<T>(staged, identity).Entities().TryGetValue(id, out var entity))
            return entity;
        if (staged.Removed.Contains(id))
            return new NotFound();
        return (await store.Entity(id))
            .Match<OneOf<T, NotFound>>(stored => NotedEntity(stored), missing => missing);
    }

    public IAsyncEnumerable<T> All() => Overlay(store.All());

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
        => Overlay(store.Matching(condition)).Where(condition.Compile());

    private T NotedEntity(Versioned<T> stored)
    {
        staged.Read[identity.Of(stored.Entity)] = stored.Version;
        return stored.Entity;
    }

    private async IAsyncEnumerable<T> Overlay(IAsyncEnumerable<T> stored)
    {
        var saved = new LatestSaved<T>(staged, identity).Entities();
        var seen = new HashSet<Guid>();
        await foreach (var entity in stored)
        {
            var id = identity.Of(entity);
            if (staged.Removed.Contains(id) && !saved.ContainsKey(id))
                continue;
            seen.Add(id);
            yield return saved.TryGetValue(id, out var replacement) ? replacement : entity;
        }
        foreach (var addition in saved)
            if (!seen.Contains(addition.Key))
                yield return addition.Value;
    }
}
