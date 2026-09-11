using Apia;

namespace Apia.File;

/// <summary>
/// A memory holding its entities on disk, one JSON file per entity type. Moving a prototype here is a
/// change of composition and nothing else.
/// <para>
/// Commit atomicity: each entity type's file is written once per commit and replaced by a rename, so a
/// commit that fails part-way cannot leave a type's file half written or emptied. Atomicity across
/// entity types is not reachable on this medium and is not attempted: a commit touching two types that
/// fails while writing the second leaves the first written.
/// </para>
/// </summary>
public sealed class FileMemory : IMemory
{
    private readonly IMemory memory;

    public FileMemory(string directory, IIdentities identities, IOverrides overrides)
    {
        var stores = new FileStores(directory, identities);
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
