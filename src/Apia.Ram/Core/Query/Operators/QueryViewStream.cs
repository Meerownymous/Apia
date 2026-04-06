namespace Apia;

public static partial class QueryExtensions
{
    public static IAsyncEnumerable<TResult> Query<TResult>(
        this IViewStreamTmp<TResult>      stream,
        Func<Query<TResult>, Query<TResult>> build)
        => stream.Query(build(new Query<TResult>()));
}
