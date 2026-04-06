namespace Apia;

public static partial class QueryExtensions
{
    public static Task<TResult> Query<TResult>(
        this IViewTmp<TResult>      view,
        Func<Query<TResult>, Query<TResult>> build)
        => view.Query(build(new Query<TResult>()));
}
