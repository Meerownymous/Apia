using System.Collections.Concurrent;
using Apia;
using JasperFx;
using Marten;
using Weasel.Core;

namespace Apia.Postgres;

/// <summary>
/// Compose a Postgres-backed IMemory via Marten.
/// A type may be registered as vault, aggregate, and/or projection; only vault types are writable via IBranch.
/// </summary>
public sealed class PostgresMemoryMap : IMemoryMap
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> vaultTypes           = new();
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

    public void RegisterStore<T>(IIdentity<T> identity) where T : notnull
    {
        vaultTypes[typeof(T)] = true;
        aggregateRegistries.TryAdd(typeof(T), new PostgresAggregateRegistry<T>());
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        var reg = (IAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        var reg = (IProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public IMemory Build() => new PostgresMemory(store, vaultTypes, aggregateRegistries, projectionRegistries);
}
