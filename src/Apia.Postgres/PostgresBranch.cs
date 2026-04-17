using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>
/// Postgres unit of work. Save/Delete stage into the Marten session; Commit flushes via SaveChangesAsync.
/// </summary>
public sealed class PostgresBranch(
    IDocumentSession session,
    IMemory memory,
    ConcurrentDictionary<Type, object> aggregateRegistries,
    ConcurrentDictionary<Type, object> projectionRegistries)
    : IBranch
{
    public IAggregateSource<T> Aggregate<T>()
        => new PostgresAggregateSource<T>(
            Registry<T, PostgresAggregateRegistry<T>>(aggregateRegistries).Handlers(),
            memory,
            session);

    public IProjectionSource<T> Projection<T>()
        => new PostgresProjectionSource<T>(
            Registry<T, PostgresProjectionRegistry<T>>(projectionRegistries).Handlers(),
            memory,
            session);

    public Task Save<T>(T entity)
    {
        session.Store(entity);
        return Task.CompletedTask;
    }

    public Task Delete<T>(Guid id)
    {
        session.Delete<T>(id);
        return Task.CompletedTask;
    }

    public Task Commit() => session.SaveChangesAsync();

    private static TRegistry Registry<T, TRegistry>(ConcurrentDictionary<Type, object> registries)
        where TRegistry : new()
        => registries.TryGetValue(typeof(T), out var r)
            ? (TRegistry)r
            : new TRegistry();
}
