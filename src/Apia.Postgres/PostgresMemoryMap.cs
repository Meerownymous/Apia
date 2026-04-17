using System.Collections.Concurrent;
using Apia;
using Marten;
using Weasel.Core;

namespace Apia.Postgres;

/// <summary>
/// Compose a Postgres-backed IMemory via Marten.
/// Each type is registered as exactly one of: vault, aggregate, or projection.
/// </summary>
public sealed class PostgresMemoryMap : IMemoryMap
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> vaultTypes          = new();
    private readonly ConcurrentDictionary<Type, object> aggregateRegistries = new();
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
        GuardRole<T>("vault");
        vaultTypes[typeof(T)] = true;
        aggregateRegistries.TryAdd(typeof(T), new PostgresAggregateRegistry<T>());
    }

    public void RegisterQuery<T, TQuery>(IAggregateSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("aggregate");
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public void RegisterQuery<T, TQuery>(IPostgresAggregateSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("aggregate");
        var reg = (PostgresAggregateRegistry<T>)aggregateRegistries.GetOrAdd(typeof(T), _ => new PostgresAggregateRegistry<T>());
        reg.Register<TQuery>((q, m, s) => source.From(q, m, s));
    }

    public void RegisterProjection<T, TQuery>(IProjectionSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("projection");
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, _) => source.From(q, m));
    }

    public void RegisterProjection<T, TQuery>(IPostgresProjectionSource<T, TQuery> source) where T : notnull
    {
        GuardRole<T>("projection");
        var reg = (PostgresProjectionRegistry<T>)projectionRegistries.GetOrAdd(typeof(T), _ => new PostgresProjectionRegistry<T>());
        reg.Register<TQuery>((q, m, s) => source.From(q, m, s));
    }

    public IMemory Build() => new PostgresMemory(store, aggregateRegistries, projectionRegistries);

    private string? RoleOf(Type type)
        => vaultTypes.ContainsKey(type) ? "vault"
            : aggregateRegistries.ContainsKey(type) ? "aggregate"
            : projectionRegistries.ContainsKey(type) ? "projection"
            : null;

    private void GuardRole<T>(string role)
    {
        var existing = RoleOf(typeof(T));
        if (existing != null && existing != role)
            throw new InvalidOperationException(
                $"{typeof(T).Name} is already registered as {existing} and cannot also be registered as {role}.");
    }
}
