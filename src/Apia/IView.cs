namespace Apia;

public interface IView<TResult, in TQuery> where TQuery : QueryRecord<TResult>
{
    /// <summary>Execute the query and stream results.</summary>
    Task<TResult> Query(TQuery query);
}
