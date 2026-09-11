namespace Apia;

/// <summary>A query returning exactly one computed result, carrying its own storage-agnostic implementation.</summary>
public interface IProjectionQuery<T> where T : notnull
{
    /// <summary>The result of this query, read through the given memory.</summary>
    Task<T> Result(IMemory memory);
}
