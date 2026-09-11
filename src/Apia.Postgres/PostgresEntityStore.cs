using System.Linq.Expressions;
using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// The persistence of entities of type T inside one Marten session. A write reaches the session
/// immediately and the database when the session saves, which is what makes a commit one transaction.
/// </summary>
public sealed class PostgresEntityStore<T>(IDocumentSession session) : IEntityStore<T> where T : notnull
{
    public async Task<OneOf<Versioned<T>, NotFound>> Entity(Guid id)
    {
        var found = await session.LoadAsync<T>(id);
        return found is null
            ? new NotFound()
            : OneOf<Versioned<T>, NotFound>.FromT0(new Versioned<T>(found, await Version(found)));
    }

    public async IAsyncEnumerable<T> All()
    {
        foreach (var entity in await session.Query<T>().ToListAsync())
            yield return entity;
    }

    public async IAsyncEnumerable<T> Matching(Expression<Func<T, bool>> condition)
    {
        foreach (var entity in await session.Query<T>().Where(condition).ToListAsync())
            yield return entity;
    }

    public Task Write(IReadOnlyCollection<T> saved, IReadOnlyCollection<Guid> removed)
    {
        foreach (var entity in saved)
            session.Store(entity);
        foreach (var id in removed)
            session.Delete<T>(id);
        return Task.CompletedTask;
    }

    private async Task<Guid> Version(T entity)
        => await session.MetadataForAsync(entity) is { } metadata
            ? metadata.CurrentVersion
            : throw new InvalidOperationException(
                $"Marten reported no metadata for the {typeof(T).Name} it had just loaded, so the version "
                + "it is stored at is unknown. A commit could not tell whether it changed underneath the "
                + "branch, and silently reporting it unchanged would be worse than failing here.");
}
