namespace Apia;

/// <summary>Staged changes whose ids are already resolved, so that writing them can no longer fail on an identity.</summary>
public interface IResolvedChanges
{
    /// <summary>Writes these changes to the store.</summary>
    Task Write();
}
