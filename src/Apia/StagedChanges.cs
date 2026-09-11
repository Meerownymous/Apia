namespace Apia;

/// <summary>The changes a branch staged for entities of type T.</summary>
public sealed class StagedChanges<T>(IEntityStore<T> store, Staged<T> staged, IIdentity<T> identity)
    : IStagedChanges where T : notnull
{
    public async Task<bool> Unchanged()
    {
        foreach (var read in staged.Read)
            if (await HasChanged(read.Key, read.Value))
                return false;
        return true;
    }

    public IResolvedChanges Resolution() => Resolution(new LatestSaved<T>(staged, identity).Entities());

    private IResolvedChanges Resolution(IReadOnlyDictionary<Guid, T> saved)
        => new ResolvedChanges<T>(
            store,
            saved.Values.ToList(),
            staged.Removed.Where(id => !saved.ContainsKey(id)).ToList());

    private async Task<bool> HasChanged(Guid id, Guid version)
        => (await store.Entity(id)).Match(stored => stored.Version != version, _ => true);
}
