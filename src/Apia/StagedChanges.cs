namespace Apia;

/// <summary>The changes a branch staged for entities of type T.</summary>
public sealed class StagedChanges<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IStagedChanges where T : notnull
{
    public async Task<bool> Unchanged()
    {
        foreach (var read in staged.Read)
            if (await Changed(read.Key, read.Value))
                return false;
        return true;
    }

    public IResolvedChanges Resolved()
        => Resolved(LatestSaved(staged.Saved, identity));

    private IResolvedChanges Resolved(Dictionary<Guid, T> saved)
        => new ResolvedChanges<T>(
            store,
            saved.Values.ToList(),
            staged.Removed.Where(id => !saved.ContainsKey(id)).ToList());

    private async Task<bool> Changed(Guid id, Guid version)
        => (await store.Entity(id)).Match(stored => stored.Version != version, _ => true);

    private static Dictionary<Guid, T> LatestSaved(IEnumerable<T> saved, IIdentity<T> identity)
    {
        var latest = new Dictionary<Guid, T>();
        foreach (var entity in saved)
            latest[identity.Of(entity)] = entity;
        return latest;
    }
}
