namespace Apia;

/// <summary>Returns exactly one computed result for a given query. No identity, no mutation.</summary>
public interface IProjectionSource<T>
{
    Task<T> From<TQuery>(TQuery query);
}

/// <summary>Returns exactly one computed result for a typed query carrying a seed of type TQuery.</summary>
public interface IProjectionSource<T, TQuery>
{
    Task<T> From(IQuery<TQuery> query, IMemory memory);
}
