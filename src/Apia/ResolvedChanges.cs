namespace Apia;

/// <summary>The changes of one entity type resolved to the ids they will be written under.</summary>
public sealed class ResolvedChanges<T>(
    IEntityStore<T> store,
    IReadOnlyCollection<T> saved,
    IReadOnlyCollection<Guid> removed)
    : IResolvedChanges where T : notnull
{
    public Task Write()
        => saved.Count == 0 && removed.Count == 0
            ? Task.CompletedTask
            : store.Write(saved, removed);
}
