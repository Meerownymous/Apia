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
        stagings.Entries<T>().Removed.Add(id);
        return Task.CompletedTask;
    }

    public async Task<OneOf<Committed, Stale>> Commit()
    {
        var writes = stagings.Changes().Select(changes => changes.Resolved()).ToList();
        foreach (var changes in stagings.Changes())
            if (!await changes.Unchanged())
                return new Stale();
        foreach (var write in writes)
            await write.Write();
        return new Committed();
    }
}
