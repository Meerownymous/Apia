namespace Apia;

public interface IProjection<TResult, TQuery> where TQuery : Query<TResult>
{
    /// <summary>Execute the query and stream results.</summary>
    IAsyncEnumerable<TResult> Query(TQuery query);
}
