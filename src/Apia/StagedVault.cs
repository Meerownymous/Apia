using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>
/// Read access to the entities of type T as a branch sees them: what it staged, layered over what the
/// store holds. Reading by id notes what the store answered — the version of the entity it held, or
/// that it held none — which is what a commit compares against. An id answered as absent is noted the
/// same way a found one is, so that two branches creating the same entity is a stale commit for the
/// second rather than a silent overwrite.
/// <para>
/// Reading through <see cref="All"/> or <see cref="Matching"/> notes nothing, so streaming a type does
/// not make every entity of it a candidate for a stale commit, nor every entity that arrives in it.
/// </para>
/// </summary>
public sealed class StagedVault<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
    {
        if (new LatestSaved<T>(staged, identity).Entities().TryGetValue(id, out var entity))
            return entity;
        // A removal the branch staged itself is absence of its own making, and the store still holds the
        // entity until the commit writes. Noting it would make every branch that reads back its own
        // delete stale against the entity it is deleting.
        if (staged.Removed.Contains(id))
            return new NotFound();
        return (await store.Entity(id))
            .Match<OneOf<T, NotFound>>(stored => NotedEntity(stored), _ => NotedAbsence(id));
    }

    public IAsyncEnumerable<T> All() => Overlay(store.All());

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
        => Overlay(store.Matching(condition)).Where(condition.Compile());

    private T NotedEntity(Versioned<T> stored)
    {
        staged.Read[identity.Of(stored.Entity)] = stored.Version;
        return stored.Entity;
    }

    private NotFound NotedAbsence(Guid id)
    {
        staged.Absent.Add(id);
        return new NotFound();
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
