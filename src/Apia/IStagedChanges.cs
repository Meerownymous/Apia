namespace Apia;

/// <summary>The changes a branch staged for one entity type, ready to be checked and written.</summary>
public interface IStagedChanges
{
    /// <summary>
    /// The entities of this type the branch read whose store no longer answers what it answered then:
    /// an id read at a version the store has moved past, and an id read as absent the store has since
    /// been given an entity for. Empty when every read still holds. An id read as absent is named no
    /// differently from one read at a version, so a caller cannot tell an arrival from a change, and an
    /// id the branch read both ways is named once.
    /// </summary>
    Task<IReadOnlyCollection<Changed>> StaleReads();

    /// <summary>These changes resolved to the ids they will be written under.</summary>
    IResolvedChanges Resolution();
}
