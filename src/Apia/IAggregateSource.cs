namespace Apia;

public interface IAggregateSource<T>
{
    IAsyncEnumerable<T> From<TQuery>(TQuery query);
}
