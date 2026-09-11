using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>
/// Read access to the entities of type T as a branch sees them: what it staged, layered over what the
/// store holds. Reading by id records the version read, which is what a commit compares against.
/// </summary>
public sealed class StagedVault<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
    {
        foreach (var entity in Enumerable.Reverse(staged.Saved))
            if (identity.Of(entity) == id)
                return entity;
        if (staged.Removed.Contains(id))
            return new NotFound();
        return (await store.Entity(id)).Match<OneOf<T, NotFound>>(stored => Recorded(stored), missing => missing);
    }

    public IAsyncEnumerable<T> All() => Layered(store.All());

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
        => Layered(store.Matching(condition)).Where(condition.Compile());

    private T Recorded(Versioned<T> stored)
    {
        staged.Read[identity.Of(stored.Entity)] = stored.Version;
        return stored.Entity;
    }

    private async IAsyncEnumerable<T> Layered(IAsyncEnumerable<T> stored)
    {
        var saved = LatestSaved();
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

    private Dictionary<Guid, T> LatestSaved()
    {
        var latest = new Dictionary<Guid, T>();
        foreach (var entity in staged.Saved)
            latest[identity.Of(entity)] = entity;
        return latest;
    }
}
