namespace Apia;

public interface IBranch
{
    IAsyncEnumerable<T> Aggregate<T>(object query) where T : notnull;
    Task<T> Projection<T>(object query) where T : notnull;

    Task Save<T>(T entity) where T : notnull;
    Task Delete<T>(Guid id) where T : notnull;

    Task Commit();
}
