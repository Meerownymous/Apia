namespace Apia;

/// <summary>
/// Describes which records of type <typeparamref name="TRecord"/> are accessible
/// for a given filter value of type <typeparamref name="TFilter"/>.
///
/// <para>
/// Implement this once — it works identically with every backend (RAM, File, Postgres).
/// Register via the memory map and activate at runtime with
/// <c>memory.Scoped(filter)</c>.
/// </para>
///
/// <code>
/// public sealed class UserPosts : IEntitiesScope&lt;Post, Guid&gt;
/// {
///     public bool Includes(Post post, Guid userId) => post.AuthorId == userId;
///     // CanWrite and CanDelete default to Includes
/// }
/// </code>
/// </summary>
public interface IEntitiesScope<TRecord, TFilter>
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="record"/> belongs to
    /// the scope defined by <paramref name="filter"/>.
    /// Controls <c>All()</c> and <c>Load()</c>.
    /// </summary>
    bool Includes(TRecord record, TFilter filter);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="record"/> may be saved
    /// within this scope. Defaults to <see cref="Includes"/> when not overridden.
    /// </summary>
    bool CanWrite(TRecord record, TFilter filter) => Includes(record, filter);

    /// <summary>
    /// Returns <see langword="true"/> when the record with the given id may be deleted
    /// within this scope. Defaults to <see cref="Includes"/> when not overridden.
    /// </summary>
    bool CanDelete(TRecord record, TFilter filter) => Includes(record, filter);
}
