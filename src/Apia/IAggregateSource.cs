namespace Apia;

/// <summary>Streams multiple results for a given query.</summary>
public interface IAggregateSource<T>
{
    IAsyncEnumerable<T> From<TQuery>(TQuery query);
}

/// <summary>Streams multiple results for a typed query carrying a seed of type TQuery.</summary>
public interface IAggregateSource<out T, in TQuery>
{
    IAsyncEnumerable<T> From(IQuery<TQuery> query, IMemory memory);
}
