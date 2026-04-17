namespace Apia;

/// <summary>Returns exactly one computed result for a given query. No identity, no mutation.</summary>
public interface IProjectionSource<T>
{
    Task<T> From<TQuery>(TQuery query);
}
