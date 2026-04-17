using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>
/// Postgres unit of work. Save/Delete stage into the Marten session; Commit flushes via SaveChangesAsync.
/// </summary>
internal sealed class PostgresBranch(
    IDocumentSession session,
    IMemory memory,
    ConcurrentDictionary<Type, object> aggregateRegistries,
    ConcurrentDictionary<Type, object> projectionRegistries)
    : IBranch
{
    public IAggregateSource<T> Aggregate<T>()
    {
        var registry = aggregateRegistries.TryGetValue(typeof(T), out var r)
            ? (PostgresAggregateRegistry<T>)r
            : new PostgresAggregateRegistry<T>();
        return new PostgresAggregateSource<T>(registry.Handlers, memory, session);
    }

    public IProjectionSource<T> Projection<T>()
    {
        var registry = projectionRegistries.TryGetValue(typeof(T), out var r)
            ? (PostgresProjectionRegistry<T>)r
            : new PostgresProjectionRegistry<T>();
        return new PostgresProjectionSource<T>(registry.Handlers, memory, session);
    }

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
}
