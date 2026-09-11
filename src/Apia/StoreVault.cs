using System.Linq.Expressions;
using OneOf;

namespace Apia;

/// <summary>Read access to the entities of type T held by one store.</summary>
public sealed class StoreVault<T>(IEntityStore<T> store) : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
        => (await store.Entity(id)).Match<OneOf<T, NotFound>>(stored => stored.Entity, missing => missing);

    public IAsyncEnumerable<T> All() => store.All();

    public IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition) => store.Matching(condition);
}
