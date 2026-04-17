namespace Apia;

/// <summary>Returns exactly one computed result for a given query. No identity, no mutation.</summary>
public interface IProjectionSource<T>
{
    Task<T> From<TQuery>(TQuery query);
}

public interface IProjectionSource<T, TQuery>
{
    Task<T> From(TQuery query, IMemory memory);
}
