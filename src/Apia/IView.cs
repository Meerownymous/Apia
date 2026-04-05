namespace Apia;

public interface IView<TResult, in TQuery> where TQuery : Query<TResult>
{
    /// <summary>Execute the query and stream results.</summary>
    Task<TResult> Query(TQuery query);
}
