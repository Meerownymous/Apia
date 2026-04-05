namespace Apia;

public interface IEntities<TRecord>
{
    /// <summary>Extracts the Guid key from a record.</summary>
    Guid IdOf(TRecord result);

    /// <summary>Streams all stored ids.</summary>
    IAsyncEnumerable<TRecord> All();

    /// <summary>Load a record by id. Throws if not found.</summary>
    Task<TRecord> Fetch(Guid id);

    /// <summary>Save a record. Insert if new, optimistic update if known.</summary>
    Task Save(TRecord record);

    /// <summary>Delete a record by id. No-op if not found.</summary>
    Task Delete(Guid id);
}
