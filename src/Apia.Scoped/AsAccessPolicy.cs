namespace Apia.Scope;

/// <summary>
/// An <see cref="IAccessPolicy{TRecord,TContext}"/> backed by three predicate functions —
/// one each for read, write, and delete operations.
/// Built and stored by <see cref="Policies{TContext}"/>.
/// </summary>
public sealed class AsAccessPolicy<TRecord, TContext>(
    Func<TRecord, TContext, bool> canRead,
    Func<TRecord, TContext, bool> canWrite,
    Func<TRecord, TContext, bool> canDelete)
: IAccessPolicy<TRecord, TContext>
{
    /// <inheritdoc/>
    public bool CanRead(TRecord record, TContext context)   => canRead(record, context);

    /// <inheritdoc/>
    public bool CanWrite(TRecord record, TContext context)  => canWrite(record, context);

    /// <inheritdoc/>
    public bool CanDelete(TRecord record, TContext context) => canDelete(record, context);
}
