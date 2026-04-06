namespace Apia.Ram.Core;

public interface IViewStreamTmp<out TResult, TQueryTarget>
{
    IAsyncEnumerable<TResult> Query(IQuery<TQueryTarget> query);
}
