namespace Apia.Ram;

/// <summary>
/// Abstract synopsis stream for RAM-backed views.
/// Subclasses pass a query function to the base constructor.
/// IMemory is provided at query time via Grow().
/// </summary>
public abstract class RamSynopsisStream<TResult, TSeed>(Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
    : ISynopsisStream<TResult, TSeed, IMemory>
    where TSeed : notnull
{
    public IViewStream<TResult, TSeed> Grow(IMemory memory)
        => new BoundViewStream(memory, query);

    /// <summary>A view stream bound to a specific IMemory instance.</summary>
    private sealed class BoundViewStream(IMemory memory, Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TSeed>
    {
        public IAsyncEnumerable<TResult> Build(TSeed seed) => query(memory, seed);
    }
}
