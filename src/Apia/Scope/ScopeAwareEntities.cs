using OneOf;
using System.Linq.Expressions;

namespace Apia.Scope;

/// <summary>
/// Decorator that enforces an <see cref="IEntitiesScope{TRecord,TFilter}"/> on any
/// <see cref="IEntities{TRecord}"/> implementation.
///
/// <para><b>Optimisation path (Postgres):</b>
/// When the scope implements <see cref="ILinqEntitiesScope{TRecord,TFilter}"/> AND the
/// inner entities implement <see cref="ILinqFilterableEntities{TRecord}"/>, <c>All()</c>
/// delegates the predicate to the storage engine (SQL WHERE) instead of post-filtering.
/// </para>
///
/// <para><b>Fallback path (RAM, File, or non-optimised Postgres):</b>
/// Iterates <c>inner.All()</c> and drops records for which
/// <see cref="IEntitiesScope{TRecord,TFilter}.Includes"/> returns <see langword="false"/>.
/// </para>
/// </summary>
internal sealed class ScopeAwareEntities<TRecord, TFilter> : IEntities<TRecord>
{
    private readonly IEntities<TRecord> inner;
    private readonly IEntitiesScope<TRecord, TFilter> scope;
    private readonly TFilter filter;

    internal ScopeAwareEntities(
        IEntities<TRecord> inner,
        IEntitiesScope<TRecord, TFilter> scope,
        TFilter filter)
    {
        this.inner  = inner;
        this.scope  = scope;
        this.filter = filter;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    /// <summary>
    /// Uses the storage-level predicate when available; otherwise post-filters.
    /// </summary>
    public IAsyncEnumerable<TRecord> All()
    {
        if (scope is ILinqEntitiesScope<TRecord, TFilter> linqScope
            && inner is ILinqFilterableEntities<TRecord> filterable)
        {
            // Postgres fast path: WHERE clause pushed into the query engine.
            return filterable.All(linqScope.Filter(filter));
        }

        // RAM / File fallback: iterate and filter in-process.
        return FilterAll();
    }

    private async IAsyncEnumerable<TRecord> FilterAll()
    {
        await foreach (var record in inner.All())
            if (scope.Includes(record, filter))
                yield return record;
    }

    /// <summary>
    /// Returns <see cref="NotFound"/> when the record exists but is outside the scope —
    /// indistinguishable from a genuine miss to prevent foreign-ID enumeration.
    /// </summary>
    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var result = await inner.Load(id);
        return result.Match<OneOf<TRecord, NotFound>>(
            record => scope.Includes(record, filter) ? record : new NotFound(),
            notFound => notFound
        );
    }

    /// <summary>Rejects the save when <c>CanWrite</c> is false for the current filter.</summary>
    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
        if (!scope.CanWrite(record, filter))
            throw new UnauthorizedAccessException(
                $"Access denied: cannot save {typeof(TRecord).Name} — CanWrite returned false for the current scope.");

        return inner.Save(record);
    }

    /// <summary>
    /// Loads the record first to verify <c>CanDelete</c>; silently no-ops for missing records.
    /// </summary>
    public async Task Delete(Guid id)
    {
        var result = await inner.Load(id);
        var allowed = result.Match(
            record => scope.CanDelete(record, filter),
            _ => true   // genuinely missing — Delete is a no-op anyway
        );

        if (!allowed)
            throw new UnauthorizedAccessException(
                $"Access denied: cannot delete {typeof(TRecord).Name} {id} — CanDelete returned false for the current scope.");

        await inner.Delete(id);
    }
}
