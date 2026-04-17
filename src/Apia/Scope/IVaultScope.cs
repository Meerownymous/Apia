namespace Apia.Scope;

/// <summary>
/// Describes which records of type <typeparamref name="TRecord"/> are visible and mutable
/// for a given filter value of type <typeparamref name="TFilter"/>.
/// </summary>
public interface IVaultScope<TRecord, TFilter>
{
    /// <summary>Whether <paramref name="record"/> is visible in this scope (controls Load).</summary>
    bool Includes(TRecord record, TFilter filter);

    /// <summary>Whether <paramref name="record"/> may be saved in this scope.</summary>
    bool CanWrite(TRecord record, TFilter filter) => Includes(record, filter);

    /// <summary>Whether the record with this id may be deleted in this scope.</summary>
    bool CanDelete(TRecord record, TFilter filter) => Includes(record, filter);
}
