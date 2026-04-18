namespace Apia;

public interface IBranch
{
    /// <summary>Streams multiple results for the given query.</summary>
    IAsyncEnumerable<TAggregated> Aggregate<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query);

    /// <summary>Returns a single computed result for the given query.</summary>
    Task<TAggregated> Projection<TAggregated, TQuery>(IQuery<TQuery, TAggregated> query);

    /// <summary>Stages an upsert for the given entity.</summary>
    Task Save<T>(T entity);

    /// <summary>Stages a delete for the given entity id.</summary>
    Task Delete<T>(Guid id);

    /// <summary>Flushes all staged operations.</summary>
    Task Commit();
}
