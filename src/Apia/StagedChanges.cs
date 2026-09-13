namespace Apia;

/// <summary>The changes a branch staged for entities of type T.</summary>
public sealed class StagedChanges<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IStagedChanges where T : notnull
{
    public async Task<IReadOnlyCollection<Changed>> StaleReads()
    {
        // A branch can hold both memories of one id — read at a version and read as absent, in either
        // order — and both can answer stale at once. Collecting ids rather than appending to a list of
        // outcomes is what keeps such an id named once.
        var stale = new HashSet<Guid>();
        foreach (var read in staged.Read)
            if (await HasChanged(read.Key, read.Value))
                stale.Add(read.Key);
        foreach (var absent in staged.Absent)
            if (await HasArrived(absent))
                stale.Add(absent);
        return stale.Select(id => new Changed(typeof(T), id)).ToList();
    }

    public IResolvedChanges Resolution() => Resolution(new LatestSaved<T>(staged, identity).Entities());

    private IResolvedChanges Resolution(IReadOnlyDictionary<Guid, T> saved)
        => new ResolvedChanges<T>(
            store,
            saved.Values.ToList(),
            staged.Removed.Where(id => !saved.ContainsKey(id)).ToList());

    private async Task<bool> HasChanged(Guid id, Guid version)
        => (await store.Entity(id)).Match(stored => stored.Version != version, _ => true);

    private async Task<bool> HasArrived(Guid id)
        => (await store.Entity(id)).Match(_ => true, _ => false);
}
