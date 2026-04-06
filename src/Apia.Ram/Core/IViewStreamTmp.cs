namespace Apia;

public interface IViewStreamTmp<TResult>
{
    IAsyncEnumerable<TResult> Query(IQuery<TResult> query);
}
