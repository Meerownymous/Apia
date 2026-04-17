namespace Apia;

public interface IBranch
{
    IAggregateSource<T> Aggregate<T>();
    IProjectionSource<T> Projection<T>();

    Task Save<T>(T entity);
    Task Delete<T>(Guid id);

    Task Commit();
}
