namespace Apia;

public interface IBranch
{
    IAsyncEnumerable<T> Aggregate<T>(object query);
    Task<T> Projection<T>(object query);

    Task Save<T>(T entity);
    Task Delete<T>(Guid id);

    Task Commit();
}
