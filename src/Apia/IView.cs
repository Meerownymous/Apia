namespace Apia;

public interface IView<TResult, in TSeed> where TSeed : notnull
{
    /// <summary>Execute the query and stream results.</summary>
    Task<TResult> Query(TSeed query);
}
