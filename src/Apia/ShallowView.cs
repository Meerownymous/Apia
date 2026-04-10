namespace Apia;

/// <summary>
/// A synopsis backed by IMemory alone, executable on any backend without registration.
/// If a backend-specific override is registered in IMemory, Query() routes there;
/// otherwise the abstract Query() provides the fallback implementation.
/// </summary>
public abstract class ShallowView<TResult, TQuery> : IView<TResult, TQuery>
    where TQuery : Query<TResult>
{
    private readonly IMemory memory;

    /// <summary>Initialises the view with the IMemory it will query against.</summary>
    protected ShallowView(IMemory memory) => this.memory = memory;

    /// <inheritdoc/>
    public Task<TResult> Query(TQuery query)
        => memory.TryView<TResult, TQuery>().Match(
            view => view.Query(query),
            _ => Query(query, memory));

    /// <summary>
    /// Fallback executed when no backend override is registered.
    /// Use IMemory for all data access — works on Ram, File, and Postgres.
    /// </summary>
    protected abstract Task<TResult> Query(TQuery query, IMemory memory);
}
