namespace Apia;

/// <summary>A query returning many results, carrying its own storage-agnostic implementation.</summary>
public interface IAggregateQuery<T> where T : notnull
{
    /// <summary>The results of this query, read through the given memory.</summary>
    IAsyncEnumerable<T> Results(IMemory memory);
}
