using System.Collections.Concurrent;
using Apia;
using Marten;
using Weasel.Core;

namespace Apia.Postgres;

/// <summary>
/// Compose a Postgres-backed IMemory via Marten.
/// Register stores and handlers, then call Build().
/// </summary>
public sealed class PostgresMemoryMap : IMemoryMap
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries  = new();
    private readonly ConcurrentDictionary<Type, object> projectionRegistries = new();

    public PostgresMemoryMap(string connectionString)
        : this(connectionString, _ => { }) { }

    public PostgresMemoryMap(string connectionString, Action<StoreOptions> configure)
    {
        store = DocumentStore.For(opts =>
        {
            opts.Connection(connectionString);
            opts.AutoCreateSchemaObjects = AutoCreate.All;
            configure(opts);
        });
    }

    public void RegisterStore<T>(Func<T, Guid> idOf) where T : notnull
    {
        aggregateRegistries.TryAdd(typeof(T), new PostgresAggregateRegistry<T>());
        projectionRegistries.TryAdd(typeof(T), new PostgresProjectionRegistry<T>());
    }

    public void RegisterQuery<T, TQuery>(Func<TQuery, IMemory, IAsyncEnumerable<T>> handler) where T : notnull
    {
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, _) => handler(q, m));
    }

    /// <summary>Register a Postgres-specific aggregate query with full session access.</summary>
    public void RegisterQuery<T, TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> handler) where T : notnull
    {
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>(handler);
    }

    public void RegisterProjection<T, TQuery>(Func<TQuery, IMemory, Task<T>> handler) where T : notnull
    {
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, _) => handler(q, m));
    }

    /// <summary>Register a Postgres-specific projection query with full session access.</summary>
    public void RegisterProjection<T, TQuery>(Func<TQuery, IMemory, IDocumentSession, Task<T>> handler) where T : notnull
    {
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>(handler);
    }

    public IMemory Build() => new PostgresMemory(store, aggregateRegistries, projectionRegistries);
}
