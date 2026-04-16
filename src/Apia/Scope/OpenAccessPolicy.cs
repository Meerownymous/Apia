namespace Apia.Scope;

/// <summary>
/// An <see cref="IAccessPolicy{TRecord,TContext}"/> that permits every operation
/// unconditionally. Used as a null-object when a query supports context injection
/// (via <see cref="IScopedQuery{TContext}"/>) but no explicit read predicate is
/// registered — the synopsis is trusted to apply all filtering natively.
/// </summary>
public sealed class OpenAccessPolicy<TRecord, TContext> : IAccessPolicy<TRecord, TContext>
{
    /// <inheritdoc/>
    public bool CanRead(TRecord record, TContext context)   => true;

    /// <inheritdoc/>
    public bool CanWrite(TRecord record, TContext context)  => true;

    /// <inheritdoc/>
    public bool CanDelete(TRecord record, TContext context) => true;
}
