namespace Apia;

/// <summary>Convenience extensions on <see cref="IMemory"/> and <see cref="IBranch"/> that infer both type parameters from the query argument.</summary>
public static class MemoryExtensions
{
    /// <summary>Streams multiple results, inferring both result and query types from the query argument.</summary>
    public static IAsyncEnumerable<TAggregated> Aggregate<TQuery, TAggregated>(
        this IMemory memory, IQuery<TQuery, TAggregated> query)
        => memory.Aggregate<TAggregated, TQuery>(query);

    /// <summary>Returns a single computed result, inferring both result and query types from the query argument.</summary>
    public static Task<TAggregated> Projection<TQuery, TAggregated>(
        this IMemory memory, IQuery<TQuery, TAggregated> query)
        => memory.Projection<TAggregated, TQuery>(query);

    /// <summary>Streams multiple results from a branch, inferring both result and query types from the query argument.</summary>
    public static IAsyncEnumerable<TAggregated> Aggregate<TQuery, TAggregated>(
        this IBranch branch, IQuery<TQuery, TAggregated> query)
        => branch.Aggregate<TAggregated, TQuery>(query);

    /// <summary>Returns a single computed result from a branch, inferring both result and query types from the query argument.</summary>
    public static Task<TAggregated> Projection<TQuery, TAggregated>(
        this IBranch branch, IQuery<TQuery, TAggregated> query)
        => branch.Projection<TAggregated, TQuery>(query);
}
