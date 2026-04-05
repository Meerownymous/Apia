namespace Apia.Ram;

public abstract class RamSynopsisEnvelope<TResult, TQuery>(Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
    : ISynopsis<TResult, TQuery, IMemory>
    where TQuery : Query<TResult>
{
    public IProjection<TResult, TQuery> Build(IMemory memory)
        => new BoundSynopsis(memory, query);

    private sealed class BoundSynopsis(IMemory memory, Func<IMemory, TQuery, IAsyncEnumerable<TResult>> query)
        : IProjection<TResult, TQuery>
    {
        public IAsyncEnumerable<TResult> Query(TQuery q) => query(memory, q);
    }
}
