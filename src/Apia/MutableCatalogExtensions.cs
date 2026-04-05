namespace Apia;

public static class MutableCatalogExtensions
{
    public static async IAsyncEnumerable<TResult> All<TResult>(this IMutableCatalog<TResult> catalog)
    {
        await foreach (var id in catalog.Ids())
            yield return await catalog.Load(id);
    }
}
