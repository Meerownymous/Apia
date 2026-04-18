namespace Apia;

/// <summary>Streams multiple results for any query producing entities of type <typeparamref name="T"/>.</summary>
public interface IAggregateSource<T>
{
    /// <summary>Streams results for the given query.</summary>
    IAsyncEnumerable<T> From<TQuery>(IQuery<TQuery, T> query);
}

/// <summary>Streams multiple results for a specific query type <typeparamref name="TQuery"/>.</summary>
public interface IAggregateSource<out T, in TQuery> where TQuery : IQuery<TQuery, T>
{
    /// <summary>Streams results for the given typed query.</summary>
    IAsyncEnumerable<T> From(TQuery query, IMemory memory);
}
