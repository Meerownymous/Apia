using OneOf;

namespace Apia;

/// <summary>Read access to the entities of a single type within a memory.</summary>
public interface IVault<T> where T : notnull
{
    /// <summary>The entity with the given id, or <see cref="NotFound"/>.</summary>
    Task<OneOf<T, NotFound>> Load(Guid id);
}
