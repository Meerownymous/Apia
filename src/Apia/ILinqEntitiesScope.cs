using System.Linq.Expressions;

namespace Apia;

/// <summary>
/// Extends <see cref="IEntitiesScope{TRecord,TFilter}"/> with a LINQ expression
/// that backends supporting queryable storage (e.g. Postgres via Marten) can push
/// into the underlying query engine as a native WHERE clause.
///
/// <para>
/// When an entity store implements <see cref="ILinqFilterableEntities{TRecord}"/>
/// and the registered scope implements this interface, <c>ScopeAwareEntities</c>
/// calls <see cref="Filter"/> and passes the expression to the store — avoiding a
/// full-table scan on large collections.
/// </para>
///
/// <para>
/// Backends that do not support queryable storage (RAM, File) transparently fall
/// back to the <see cref="IEntitiesScope{TRecord,TFilter}.Includes"/> predicate.
/// </para>
///
/// <code>
/// public sealed class UserPosts : ILinqEntitiesScope&lt;Post, UserContext&gt;
/// {
///     public bool Includes(Post p, UserContext ctx) => ctx.IsAdmin || p.AuthorId == ctx.Id;
///
///     public Expression&lt;Func&lt;Post, bool&gt;&gt; Filter(UserContext ctx)
///         => ctx.IsAdmin ? _ => true : p => p.AuthorId == ctx.Id;
/// }
/// </code>
/// </summary>
public interface ILinqEntitiesScope<TRecord, TFilter> : IEntitiesScope<TRecord, TFilter>
{
    /// <summary>
    /// A LINQ expression equivalent to <see cref="IEntitiesScope{TRecord,TFilter}.Includes"/>
    /// for the given <paramref name="filter"/>.
    /// Passed directly to <c>IQueryable.Where()</c> by supporting backends.
    /// </summary>
    Expression<Func<TRecord, bool>> Filter(TFilter filter);
}
