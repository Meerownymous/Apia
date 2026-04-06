using OneOf;

namespace Apia;

public interface IEntitiesTmp<TRecord>
{
    /// <summary>Extracts the Guid key from a record.</summary>
    Guid IdOf(TRecord result);

    /// <summary>Load a record by id. Returns NotFound if missing.</summary>
    Task<OneOf<TRecord, NotFound>> Load(Guid id);

    /// <summary>Save a record. Insert if new, optimistic update if known. Returns Conflict if modified since last Load.</summary>
    Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record);

    /// <summary>Delete a record by id. No-op if not found.</summary>
    Task Delete(Guid id);
    
    IAsyncEnumerable<TRecord> Find(IQuery<TRecord> query);
}
