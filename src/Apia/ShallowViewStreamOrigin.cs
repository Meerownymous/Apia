namespace Apia;

/// <summary>
/// Shallow Synopsis sourced from Apia memory object.
/// Use for fast feature shipping, replace by optimized version where applicable.
/// </summary>
public abstract class ShallowViewStreamOriginOrigin<TResult, TSeed>(Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
    : IViewStreamOrigin<TResult, TSeed, IMemory>
    where TSeed : notnull
{
    public IViewStream<TResult, TSeed> Grow(IMemory memory)
        => new BoundViewStream(memory, query);

    /// <summary>A view stream bound to a specific IMemory instance.</summary>
    private sealed class BoundViewStream(IMemory memory, Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TSeed>
    {
        public IAsyncEnumerable<TResult> Assemble(TSeed seed) => query(memory, seed);
    }
}