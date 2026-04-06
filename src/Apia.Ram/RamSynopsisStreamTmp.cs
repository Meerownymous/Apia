namespace Apia.Ram;

public abstract class RamSynopsisStreamTmp<TResult>(
    Func<IMemoryTmp, IQuery<TResult>, IAsyncEnumerable<TResult>> filter
): ISynopsisStreamTmp<TResult, IMemoryTmp>
{
    public IViewStreamTmp<TResult> Build(IMemoryTmp memory)
        => new RamBoundViewStream<TResult>(memory, filter);
}
