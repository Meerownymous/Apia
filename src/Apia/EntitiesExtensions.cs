namespace Apia;

public static class EntitiesExtensions
{
    public static async IAsyncEnumerable<TResult> All<TResult>(this IEntities<TResult> entities)
    {
        await foreach (var id in entities.Ids())
            yield return await entities.Load(id);
    }
}
