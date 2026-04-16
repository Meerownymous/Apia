namespace Apia.Scope;

/// <summary>
/// The access rules for a single record type within a given context.
/// Consumed by <see cref="PolicyEntities{TRecord,TContext}"/>,
/// <see cref="PolicyView{TResult,TQuery,TContext}"/>, and
/// <see cref="PolicyViewStream{TResult,TQuery,TContext}"/>.
/// </summary>
public interface IAccessPolicy<in TRecord, in TContext>
{
    /// <summary>Whether the given record may be read in this context.</summary>
    bool CanRead(TRecord record, TContext context);

    /// <summary>Whether the given record may be written in this context.</summary>
    bool CanWrite(TRecord record, TContext context);

    /// <summary>Whether the given record may be deleted in this context.</summary>
    bool CanDelete(TRecord record, TContext context);
}
