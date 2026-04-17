namespace Apia;

public interface IAggregateSource<T>
{
    IAsyncEnumerable<T> From<TQuery>(TQuery query);
}

public interface IAggregateSource<T, TQuery>
{
    IAsyncEnumerable<T> From(TQuery query, IMemory memory);
}
