namespace Apia;

/// <summary>
/// A unit of work. Changes staged on it reach the stores only on commit.
/// </summary>
public interface IBranch
{
    /// <summary>The results of a multi-result query over entities of type T.</summary>
    IAsyncEnumerable<T> Aggregate<T>(object query) where T : notnull;

    /// <summary>The single computed result of a query.</summary>
    Task<T> Projection<T>(object query) where T : notnull;

    /// <summary>Stages the given entity, replacing any staged or committed entity with the same id.</summary>
    Task Save<T>(T entity) where T : notnull;

    /// <summary>Stages the removal of the entity of type T with the given id.</summary>
    Task Delete<T>(Guid id) where T : notnull;

    /// <summary>Flushes every staged change to the stores.</summary>
    Task Commit();
}
