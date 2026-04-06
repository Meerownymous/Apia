using Apia.Ram.Core;

namespace Apia.Ram;

/// <summary>
/// Binds a ViewStream with a query to a memory. 
/// </summary>
public sealed class RamBoundViewStream<TResult, TQueryTarget>(
    IMemoryTmp memory,
    Func<IMemoryTmp, IQuery<TQueryTarget>, IAsyncEnumerable<TResult>> bind
) : IViewStreamTmp<TResult, TQueryTarget>
{
    public IAsyncEnumerable<TResult> Query(IQuery<TQueryTarget> query) => bind(memory, query);
}