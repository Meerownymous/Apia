using OneOf;

namespace Apia;

/// <summary>
/// A unit of work over the stores of one backend. Every staged change is resolved to the id it will be
/// written under before any store is touched, so an entity that cannot be identified costs the commit
/// nothing rather than half of it.
/// </summary>
public sealed class StoreBranch(
    IStores stores,
    IIdentities identities,
    IOverrides overrides,
    IBranches branches)
    : IBranch
{
    private readonly IStagings stagings = new Stagings(stores, identities);

    public IMemory Memory()
        => new Memory(new StagedVaults(stores, stagings, identities), branches, overrides);

    public Task Save<T>(T entity) where T : notnull
    {
        stagings.Entries<T>().Saved.Add(entity);
        return Task.CompletedTask;
    }

    public Task Delete<T>(Guid id) where T : notnull
    {
        Remove(stagings.Entries<T>(), new GivenIdentity<T>(identities), id);
        return Task.CompletedTask;
    }

    public async Task<OneOf<Committed, Stale>> Commit()
    {
        // Resolving every change first is what makes the commit all or nothing: an entity that cannot
        // be identified throws here, before the first store has been written to.
        var resolved = stagings.Changes().Select(changes => changes.Resolution()).ToList();
        foreach (var changes in stagings.Changes())
            if (!await changes.Unchanged())
                return new Stale();
        foreach (var write in resolved)
            await write.Write();
        return new Committed();
    }

    private static void Remove<T>(Staged<T> staged, IIdentity<T> identity, Guid id) where T : notnull
    {
        staged.Saved.RemoveAll(entity => identity.Of(entity) == id);
        staged.Removed.Add(id);
    }
}
