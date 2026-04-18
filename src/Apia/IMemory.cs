namespace Apia;

public interface IMemory
{
    /// <summary>Streams multiple results for the given query.</summary>
    IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query);

    /// <summary>Returns a single computed result for the given query.</summary>
    Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query);

    /// <summary>Returns a read-only vault for the given entity type.</summary>
    IVault<T> Vault<T>();

    /// <summary>Returns a new branch for staging and committing changes.</summary>
    IBranch Branch();
}
