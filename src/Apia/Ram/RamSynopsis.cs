namespace Apia.Ram;

public abstract class RamSynopsis<TResult, TQuery>
    : ISynopsis<TResult, TQuery, IMemory>
    where TQuery : Query<TResult>
{
    public IProjection<TResult, TQuery> Build(IMemory memory)
        => new BoundProjection(memory, Query);

    protected abstract IAsyncEnumerable<TResult> Query(IMemory memory, TQuery query);

    private sealed class BoundProjection(IMemory memory, Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
        : IProjection<TResult, TQuery>
    {
        public IAsyncEnumerable<TResult> Query(TQuery q) => query(memory, q);
    }
}
