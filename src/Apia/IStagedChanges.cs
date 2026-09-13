namespace Apia;

/// <summary>The changes a branch staged for one entity type, ready to be checked and written.</summary>
public interface IStagedChanges
{
    /// <summary>
    /// The entities of this type the branch read that no longer hold the version they were read at,
    /// empty when every read still holds it.
    /// </summary>
    Task<IReadOnlyCollection<Changed>> StaleReads();

    /// <summary>These changes resolved to the ids they will be written under.</summary>
    IResolvedChanges Resolution();
}
