namespace Apia;

public interface IViewStream<out TResult, in TQuery> where TQuery : Query<TResult>
{
    /// <summary>Execute the query and stream results.</summary>
    IAsyncEnumerable<TResult> Query(TQuery query);
}
