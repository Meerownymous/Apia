namespace Apia;

/// <summary>The entities a branch has staged for saving, one per id.</summary>
public interface ILatestSaved<T> where T : notnull
{
    /// <summary>The last entity staged under each id, by id.</summary>
    IReadOnlyDictionary<Guid, T> Entities();
}
