using OneOf;

namespace Apia;

public interface IMutable<TResult>
{
    /// <summary>Load the single record. Returns NotFound if not yet saved.</summary>
    Task<OneOf<TResult, NotFound>> Load();

    /// <summary>Save the record. Throws ConcurrentModificationException on conflict.</summary>
    Task Save(TResult record);
}
