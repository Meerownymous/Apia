using System.Linq.Expressions;

namespace Apia;

/// <summary>
/// Optional capability interface for entity stores that can apply a LINQ predicate
/// at the storage level — pushing the filter into the query engine rather than
/// iterating all records and discarding non-matching ones in-process.
///
/// <para>
/// This interface is the backend's side of the <see cref="ILinqEntitiesScope{TRecord,TFilter}"/>
/// optimisation contract. When both the scope and the entity store implement their
/// respective interfaces, <c>ScopeAwareEntities</c> uses the SQL path automatically.
/// </para>
///
/// Intended to be implemented by Postgres entity stores (Marten-backed).
/// RAM and File stores do not implement this interface and fall back to post-filtering.
/// </summary>
public interface ILinqFilterableEntities<TRecord>
{
    /// <summary>
    /// Streams only records matching <paramref name="predicate"/>, using the
    /// backend's native query mechanism (e.g. SQL WHERE via Marten).
    /// </summary>
    IAsyncEnumerable<TRecord> All(Expression<Func<TRecord, bool>> predicate);
}
