namespace Apia;

/// <summary>
/// Shallow Synopsis sourced from Apia memory object.
/// Use for fast feature shipping, replace by optimized version where applicable.
/// </summary>
public abstract class ShallowViewOrigin<TResult, TSeed>(Func<IMemory, TSeed, Task<TResult>> query)
    : IViewOrigin<TResult, TSeed, IMemory>
    where TSeed : notnull
{
    public IView<TResult, TSeed> Assemble(IMemory memory)
        => new BoundView(memory, query);

    /// <summary>A view stream bound to a specific IMemory instance.</summary>
    private sealed class BoundView(IMemory memory, Func<IMemory, TSeed, Task<TResult>> query)
        : IView<TResult, TSeed>
    {
        public Task<TResult> Query(TSeed seed) => query(memory, seed);
    }
}