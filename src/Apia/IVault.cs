using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>Read access to the entities of a single type within a memory.</summary>
public interface IVault<T> where T : notnull
{
    /// <summary>The entity with the given id, or <see cref="NotFound"/> when the memory holds none.</summary>
    Task<OneOf<T, NotFound>> Entity(Guid id);

    /// <summary>Every entity of type T the memory holds.</summary>
    IAsyncEnumerable<T> All();

    /// <summary>
    /// Every entity of type T satisfying the given condition. This is the single channel through which
    /// a backend pushes filtering into its own query language.
    /// </summary>
    IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition);
}
