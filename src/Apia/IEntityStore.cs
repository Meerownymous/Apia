using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>The persistence of a single entity type inside one backend. Below the vault, never seen by a use case.</summary>
public interface IEntityStore<T> where T : notnull
{
    /// <summary>The stored entity with the given id at the version it is stored at, or <see cref="NotFound"/>.</summary>
    Task<OneOf<Versioned<T>, NotFound>> Entity(Guid id);

    /// <summary>Every stored entity of type T.</summary>
    IAsyncEnumerable<T> All();

    /// <summary>Every stored entity of type T satisfying the given condition.</summary>
    IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition);

    /// <summary>
    /// Stores the given entities and removes the given ids in a single write, giving every stored
    /// entity a fresh version. A write that fails leaves every already stored entity as it was.
    /// </summary>
    Task Write(IReadOnlyCollection<T> saved, IReadOnlyCollection<Guid> removed);
}
