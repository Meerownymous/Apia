using OneOf;

namespace Apia;

/// <summary>A persistent store for entities of type T, keyed by Guid.</summary>
public interface IEntityStore<T> where T : notnull
{
    /// <summary>The entity with the given id, or NotFound.</summary>
    Task<OneOf<T, NotFound>> Get(Guid id);

    /// <summary>All entities currently in the store.</summary>
    IAsyncEnumerable<T> All();

    /// <summary>
    /// Persists the given entity, replacing any existing entry with the same id. A call that fails
    /// leaves every already stored entity as it was.
    /// </summary>
    Task Set(T entity);

    /// <summary>
    /// Removes the entity with the given id. A call that fails leaves every already stored entity as
    /// it was.
    /// </summary>
    Task Remove(Guid id);
}
