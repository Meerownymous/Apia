namespace Apia;

/// <summary>The changes a branch staged for one entity type, ready to be checked and written.</summary>
public interface IStagedChanges
{
    /// <summary>Whether every entity the branch read by id still holds the version it was read at.</summary>
    Task<bool> Unchanged();

    /// <summary>These changes resolved to the ids they will be written under.</summary>
    IResolvedChanges Resolution();
}
