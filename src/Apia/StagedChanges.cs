namespace Apia;

/// <summary>The changes a branch staged for entities of type T.</summary>
public sealed class StagedChanges<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IStagedChanges where T : notnull
{
    public async Task<IReadOnlyCollection<Changed>> StaleReads()
    {
        var stale = new List<Changed>();
        foreach (var read in staged.Read)
            if (await HasChanged(read.Key, read.Value))
                stale.Add(new Changed(typeof(T), read.Key));
        // An id read both at a version and as absent was deleted underneath the branch between the two
        // reads, which the version read already reports: every version is fresh, so the store answers
        // either nothing or a version the branch never read. Skipping it keeps the id named once.
        foreach (var absent in staged.Absent.Where(id => !staged.Read.ContainsKey(id)))
            if (await HasArrived(absent))
                stale.Add(new Changed(typeof(T), absent));
        return stale;
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
