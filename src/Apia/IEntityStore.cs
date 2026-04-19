using OneOf;

namespace Apia;

/// <summary>A persistent store for entities of type T, keyed by string id.</summary>
public interface IEntityStore<T>
{
    /// <summary>The entity with the given id, or NotFound.</summary>
    Task<OneOf<T, NotFound>> Get(string id);

    /// <summary>All entities currently in the store.</summary>
    IAsyncEnumerable<T> All();

    /// <summary>Persists the given entity, replacing any existing entry with the same id.</summary>
    Task Set(T entity);

    /// <summary>Removes the entity with the given id.</summary>
    Task Remove(string id);
}
