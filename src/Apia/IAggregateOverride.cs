namespace Apia;

/// <summary>
/// A backend-specific implementation of one aggregate query, chosen when the memory is composed and
/// never visible to a use case. It reads past the vault and therefore past every scope — see
/// docs/adr/0001-backend-overrides-bypass-scopes.md.
/// </summary>
public interface IAggregateOverride<in TQuery, T> where TQuery : IAggregateQuery<T> where T : notnull
{
    /// <summary>The results of the given query, produced by this backend rather than by the query itself.</summary>
    IAsyncEnumerable<T> Results(TQuery query, IMemory memory);
}
