namespace Apia.Ram;

public abstract class RamSynopsis<TResult, TQuery>(Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
    : ISynopsis<TResult, TQuery, IMemory>
    where TQuery : Query<TResult>
{
    public IViews<TResult, TQuery> Build(IMemory memory)
        => new BoundProjection(memory, query);

    private sealed class BoundProjection(IMemory memory, Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
        : IViews<TResult, TQuery>
    {
        public IAsyncEnumerable<TResult> Query(TQuery q) => query(memory, q);
    }
}
