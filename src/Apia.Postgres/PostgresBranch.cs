using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>
/// Postgres unit of work. Save/Delete stage into the Marten session; Commit flushes via SaveChangesAsync.
/// Only types registered via RegisterStore are writable.
/// </summary>
public sealed class PostgresBranch(
    IDocumentSession session,
    IMemory memory,
    ConcurrentDictionary<Type, object> vaultTypes,
    ConcurrentDictionary<Type, object> aggregateRegistries,
    ConcurrentDictionary<Type, object> projectionRegistries)
    : IBranch
{
    public IAggregateSource<T> Aggregate<T>()
        => new PostgresAggregateSource<T>(
            AggregateRegistry<T>().Sources(),
            memory,
            session);

    public IProjectionSource<T> Projection<T>()
        => new PostgresProjectionSource<T>(
            ProjectionRegistry<T>().Sources(),
            memory,
            session);

    public Task Save<T>(T entity)
        => vaultTypes.ContainsKey(typeof(T))
            ? Task.FromResult(session.Store(entity))
            : throw new InvalidOperationException($"{typeof(T).Name} has no registered store and cannot be saved.");

    public Task Delete<T>(Guid id)
        => vaultTypes.ContainsKey(typeof(T))
            ? Task.FromResult(session.Delete<T>(id))
            : throw new InvalidOperationException($"{typeof(T).Name} has no registered store and cannot be deleted.");

    public Task Commit() => session.SaveChangesAsync();

    private IAggregateRegistry<T> AggregateRegistry<T>()
        => aggregateRegistries.TryGetValue(typeof(T), out var r)
            ? (IAggregateRegistry<T>)r
            : new PostgresAggregateRegistry<T>();

    private IProjectionRegistry<T> ProjectionRegistry<T>()
        => projectionRegistries.TryGetValue(typeof(T), out var r)
            ? (IProjectionRegistry<T>)r
            : new PostgresProjectionRegistry<T>();
}
