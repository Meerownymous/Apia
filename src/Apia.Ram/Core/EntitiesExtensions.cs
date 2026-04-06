using OneOf;
using Tonga.Enumerable;

namespace Apia.Ram.Core;

public static partial class EntitiesExtensions
{
    public static async Task<OneOf<TEntity, NotFound, Ambiguous<TEntity>>> FindSingle<TEntity>(this IEntitiesTmp<TEntity> entitiesTmp, IQuery<TEntity> query)
    {
        OneOf<TEntity, NotFound, Ambiguous<TEntity>> result = new NotFound();
        var items = await entitiesTmp.Find(query).ToListAsync();
        if (items.Count > 1)
            result = new Ambiguous<TEntity>(items);
        else
            result = items.FirstOne().Value();
        return result;
    }
}