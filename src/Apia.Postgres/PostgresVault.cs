using System.Linq.Expressions;
using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>Read access to the entities of type T held by Postgres, through a session per read.</summary>
public sealed class PostgresVault<T>(IDocumentStore store) : IVault<T> where T : notnull
{
    public async Task<OneOf<T, NotFound>> Entity(Guid id)
    {
        await using var session = store.QuerySession();
        var found = await session.LoadAsync<T>(id);
        return found is null
            ? new NotFound()
            : OneOf<T, NotFound>.FromT0(found);
    }

    public async IAsyncEnumerable<T> All()
    {
        await using var session = store.QuerySession();
        foreach (var entity in await session.Query<T>().ToListAsync())
            yield return entity;
    }

    public async IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
    {
        await using var session = store.QuerySession();
        foreach (var entity in await session.Query<T>().Where(condition).ToListAsync())
            yield return entity;
    }
}
