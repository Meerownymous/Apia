namespace Apia;

/// <summary>The last entity staged under each id, so that a second save of an id replaces the first.</summary>
public sealed class LatestSaved<T>(Staged<T> staged, IIdentity<T> identity) : ILatestSaved<T> where T : notnull
{
    public IReadOnlyDictionary<Guid, T> Entities()
    {
        var latest = new Dictionary<Guid, T>();
        foreach (var entity in staged.Saved)
            latest[identity.Of(entity)] = entity;
        return latest;
    }
}
