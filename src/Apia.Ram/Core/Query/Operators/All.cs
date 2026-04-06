namespace Apia;

public static partial class QueryExtensions
{
    public static IAsyncEnumerable<T> All<T>(this IEntitiesTmp<T> entities)
        => entities.Find(new Query<T>());
}
