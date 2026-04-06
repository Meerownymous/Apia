namespace Apia.Ram;

/// <summary>
/// Binds a ViewStream with a query to a memory. 
/// </summary>
public sealed class RamBoundViewStream<TResult>(
    IMemoryTmp memory,
    Func<IMemoryTmp, IQuery<TResult>, IAsyncEnumerable<TResult>> bind
) : IViewStreamTmp<TResult>
{
    public IAsyncEnumerable<TResult> Query(IQuery<TResult> query) => bind(memory, query);
}