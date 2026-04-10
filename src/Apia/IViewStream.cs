namespace Apia;

public interface IViewStream<out TResult, in TSeed> where TSeed : notnull
{
    /// <summary>The stream of results for the given seed.</summary>
    IAsyncEnumerable<TResult> From(TSeed seed);
}
