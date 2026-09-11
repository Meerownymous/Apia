namespace Apia;

/// <summary>Returns exactly one computed result for a query. No identity, no mutation.</summary>
public interface IProjectionSource<T> where T : notnull
{
    Task<T> From(object query);
}

/// <summary>Returns exactly one computed result for a typed query carrying a seed of type TQuery.</summary>
public interface IProjectionSource<T, TQuery> where T : notnull
{
    Task<T> From(IQuery<TQuery> query, IMemory memory);
}
