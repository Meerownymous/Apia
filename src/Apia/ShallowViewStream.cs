namespace Apia;

/// <summary>
/// A synopsis backed by IMemory alone, executable on any backend without registration.
/// If a backend-specific override is registered in IMemory, From() routes there;
/// otherwise Query() provides the fallback implementation.
/// </summary>
public abstract class ShallowViewStream<TResult, TQuery> : IViewStream<TResult, TQuery>
    where TQuery : Query<TResult>
{
    private readonly IMemory memory;

    /// <summary>Initialises the view with the IMemory it will query against.</summary>
    protected ShallowViewStream(IMemory memory) => this.memory = memory;

    /// <inheritdoc/>
    public IAsyncEnumerable<TResult> From(TQuery query)
        => memory.TryViewStream<TResult, TQuery>().Match(
            stream => stream.From(query),
            _ => Query(query, memory));

    /// <summary>
    /// Fallback executed when no backend override is registered.
    /// Use IMemory for all data access — works on Ram, File, and Postgres.
    /// </summary>
    protected abstract IAsyncEnumerable<TResult> Query(TQuery query, IMemory memory);
}
