namespace Apia;

public interface IViewStream<out TResult, in TSeed> where TSeed : notnull
{
    IAsyncEnumerable<TResult> Build(TSeed seed);
}
