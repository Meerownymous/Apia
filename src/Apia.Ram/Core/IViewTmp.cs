namespace Apia;

public interface IViewTmp<TResult>
{
    Task<TResult> Query(IQuery<TResult> query);
}
