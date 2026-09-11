using Apia;

namespace Apia.Ram;

/// <summary>
/// A memory holding its entities in process. Costs nothing to stand up: the identities of the stored
/// types are the whole configuration. A commit is all or nothing — every staged change is resolved
/// before any store is written, and applying resolved changes in process cannot fail.
/// </summary>
public sealed class RamMemory : IMemory
{
    private readonly IMemory memory;

    public RamMemory(IIdentities identities, IOverrides overrides)
    {
        var stores = new RamStores(identities);
        memory = new Memory(
            new StoreVaults(stores),
            new StoreBranches(stores, identities, overrides),
            overrides);
    }

    public IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query) where T : notnull => memory.Aggregate(query);

    public Task<T> Projection<T>(IProjectionQuery<T> query) where T : notnull => memory.Projection(query);

    public IVault<T> Vault<T>() where T : notnull => memory.Vault<T>();

    public IBranch Branch() => memory.Branch();
}
