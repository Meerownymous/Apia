namespace Apia.Ram.Core.Query.Operators;

public static partial class QueryExtensions
{
    public static IAsyncEnumerable<TResult> Query<TResult, TQueryTarget>(
        this IViewStreamTmp<TResult, TQueryTarget> stream,
        Func<Query<TQueryTarget>, Query<TQueryTarget>> build
    ) => stream.Query(build(new Query<TQueryTarget>()));
}
