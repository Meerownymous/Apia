using Apia.Ram.Core;

namespace Apia.Ram;

public abstract class RamSynopsisStreamTmp<TResult, TQueryTarget>(
    Func<IMemoryTmp, IQuery<TQueryTarget>, IAsyncEnumerable<TResult>> filter
): ISynopsisStreamTmp<TResult, TQueryTarget, IMemoryTmp>
{
    public IViewStreamTmp<TResult, TQueryTarget> Build(IMemoryTmp memory)
        => new RamBoundViewStream<TResult, TQueryTarget>(memory, filter);
}
