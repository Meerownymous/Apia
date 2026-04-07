namespace Apia.Ram;

public abstract class RamSynopsisStream<TResult, TSeed>(Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
    : ISynopsisStream<TResult, TSeed, IMemory>
    where TSeed : notnull
{
    public IViewStream<TResult, TSeed> Grow(IMemory memory)
        => new BoundViewStream(memory, query);

    private sealed class BoundViewStream(IMemory memory, Func<IMemory, TSeed, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TSeed>
    {
        public IAsyncEnumerable<TResult> Build(TSeed q) => query(memory, q);
    }
}
