using OneOf;

namespace Apia;

public interface IVault<TResult>
{
    /// <summary>Load the single record. Returns NotFound if not yet saved.</summary>
    Task<OneOf<TResult, NotFound>> Load();

    /// <summary>Save the record. Returns Conflict if modified since last Load.</summary>
    Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record);
}
