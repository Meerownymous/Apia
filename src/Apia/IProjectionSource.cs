namespace Apia;

/// <summary>Returns exactly one computed result for any query producing a value of type <typeparamref name="T"/>.</summary>
public interface IProjectionSource<T>
{
    /// <summary>Returns a single computed result for the given query.</summary>
    Task<T> From<TQuery>(IQuery<TQuery, T> query);
}

/// <summary>Returns exactly one computed result for a specific query type <typeparamref name="TQuery"/>.</summary>
public interface IProjectionSource<T, in TQuery> where TQuery : IQuery<TQuery, T>
{
    /// <summary>Returns a single computed result for the given typed query.</summary>
    Task<T> From(TQuery query, IMemory memory);
}
