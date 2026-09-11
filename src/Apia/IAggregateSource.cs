namespace Apia;

/// <summary>Streams multiple results for a query.</summary>
public interface IAggregateSource<T> where T : notnull
{
    IAsyncEnumerable<T> From(object query);
}

/// <summary>Streams multiple results for a typed query carrying a seed of type TQuery.</summary>
public interface IAggregateSource<out T, TQuery> where T : notnull
{
    IAsyncEnumerable<T> From(IQuery<TQuery> query, IMemory memory);
}
