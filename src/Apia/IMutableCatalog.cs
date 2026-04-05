namespace Apia;

public interface IMutableCatalog<TResult>
{
    /// <summary>Extracts the Guid key from a record.</summary>
    Func<TResult, Guid> IdOf { get; }

    /// <summary>Streams all stored ids.</summary>
    IAsyncEnumerable<Guid> Ids();

    /// <summary>Load a record by id. Throws if not found.</summary>
    Task<TResult> Load(Guid id);

    /// <summary>Save a record. Insert if new, optimistic update if known.</summary>
    Task Save(TResult record);

    /// <summary>Delete a record by id. No-op if not found.</summary>
    Task Delete(Guid id);
}
