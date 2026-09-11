namespace Apia;

/// <summary>
/// A backend-specific implementation of one projection query, chosen when the memory is composed and
/// never visible to a use case. It reads past the vault and therefore past every scope — see
/// docs/adr/0001-backend-overrides-bypass-scopes.md.
/// </summary>
public interface IProjectionOverride<in TQuery, T> where TQuery : IProjectionQuery<T> where T : notnull
{
    /// <summary>The result of the given query, produced by this backend rather than by the query itself.</summary>
    Task<T> Result(TQuery query, IMemory memory);
}
