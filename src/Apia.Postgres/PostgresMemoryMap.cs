using System.Collections.Concurrent;
using Apia;
using Marten;
using Weasel.Core;

namespace Apia.Postgres;

/// <summary>
/// Compose a Postgres-backed IMemory via Marten.
/// Register stores and sources, then call Build().
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

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public void RegisterQuery<T, TQuery>(IPostgresAggregateSource<T, TQuery> source) where T : notnull
    {
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, s) => source.From(q, m, s));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IPostgresProjectionSource<T, TQuery> source) where T : notnull
    {
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, s) => source.From(q, m, s));
    }

    public IMemory Build() => new PostgresMemory(store, aggregateRegistries, projectionRegistries);
}
