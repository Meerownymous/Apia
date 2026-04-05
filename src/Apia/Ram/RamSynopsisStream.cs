namespace Apia.Ram;

public abstract class RamSynopsisStream<TResult, TQuery>(Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
    : ISynopsisStream<TResult, TQuery, IMemory>
    where TQuery : Query<TResult>
{
    public IViewStream<TResult, TQuery> Build(IMemory memory)
        => new BoundViewStream(memory, query);

    private sealed class BoundViewStream(IMemory memory, Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
        : IViewStream<TResult, TQuery>
    {
        public IAsyncEnumerable<TResult> Query(TQuery q) => query(memory, q);
    }
}
