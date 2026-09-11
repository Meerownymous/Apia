namespace Apia;

/// <summary>
/// A memory finished the moment it is constructed. A query runs against this memory, so a query reads
/// whatever this memory reads — the branch overlay and the scope decorator included.
/// </summary>
public sealed class Memory(IVaults vaults, IBranches branches, IOverrides overrides) : IMemory
{
    public IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query) where T : notnull
        => overrides.Results(query, this).Match(results => results, _ => query.Results(this));

    public Task<T> Projection<T>(IProjectionQuery<T> query) where T : notnull
        => overrides.Result(query, this).Match(result => result, _ => query.Result(this));

    public IVault<T> Vault<T>() where T : notnull => vaults.Vault<T>();

    public IBranch Branch() => branches.Branch();
}
