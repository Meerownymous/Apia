namespace Apia;

/// <summary>The staged changes of one branch over the stores of one backend.</summary>
public sealed class Stagings(IStores stores, IIdentities identities) : IStagings
{
    private readonly Dictionary<Type, object> entries = new();
    private readonly List<IStagedChanges> changes = new();

    public Staged<T> Entries<T>() where T : notnull
    {
        if (entries.TryGetValue(typeof(T), out var touched))
            return (Staged<T>)touched;
        var created =
            new Staged<T>(new List<T>(), new HashSet<Guid>(), new Dictionary<Guid, Guid>(), new HashSet<Guid>());
        entries[typeof(T)] = created;
        changes.Add(new StagedChanges<T>(stores.Store<T>(), created, new GivenIdentity<T>(identities)));
        return created;
    }

    public IEnumerable<IStagedChanges> Changes() => changes;
}
