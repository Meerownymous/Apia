using OneOf;

namespace Apia;

/// <summary>A unit of work. Changes staged on it reach the stores only on commit.</summary>
public interface IBranch
{
    /// <summary>
    /// A memory reading this branch's staged changes layered over the committed state, so that a write
    /// followed by a read inside one unit of work returns what was written.
    /// </summary>
    IMemory Memory();

    /// <summary>Stages the given entity, replacing any staged or stored entity with the same id.</summary>
    Task Save<T>(T entity) where T : notnull;

    /// <summary>Stages the removal of the entity of type T with the given id.</summary>
    Task Delete<T>(Guid id) where T : notnull;

    /// <summary>
    /// Writes every staged change, or reports <see cref="Stale"/> naming every entity this branch read
    /// by id that changed since it was read, in which case nothing is written. Expected once per branch.
    /// </summary>
    Task<OneOf<Committed, Stale>> Commit();
}
