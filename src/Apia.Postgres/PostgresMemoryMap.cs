using System.Collections.Concurrent;
using Apia;
using JasperFx;
using Marten;
using Weasel.Core;

namespace Apia.Postgres;

/// <summary>
/// Compose a Postgres-backed IMemory via Marten.
/// Synopsis sources receive (IMemory Memory, IDocumentSession Session) as context.
/// Configure schema indices via the constructor's Action&lt;StoreOptions&gt;.
/// </summary>
public sealed class PostgresMemoryMap : IMemoryMap
{
    private readonly IDocumentStore store;
    private readonly ConcurrentDictionary<Type, object> entities = new();
    private readonly ConcurrentDictionary<Type, object> vaults   = new();
    private readonly ConcurrentDictionary<(Type, Type), object> sources = new();

    public PostgresMemoryMap(string connectionString)
        : this(connectionString, _ => { })
    {
    }

    public PostgresMemoryMap(string connectionString, Action<StoreOptions> configure)
    {
        store = DocumentStore.For(opts =>
        {
            opts.Connection(connectionString);
            opts.AutoCreateSchemaObjects = AutoCreate.All;

            opts.Schema.For<ApiaVersion>()
                .Index(v => v.RecordType)
                .Index(v => v.RecordId);

            configure(opts);
        });
    }

    /// <inheritdoc/>
    public void Register<TResult>(IEntities<TResult> e) where TResult : notnull
        => entities[typeof(TResult)] = (Func<IDocumentSession, IEntities<TResult>>)(
            session => new BoundPostgresEntities<TResult>(session, e.IdOf));

    /// <inheritdoc/>
    public void Register<TResult>(IVault<TResult> vault) where TResult : notnull
        => vaults[typeof(TResult)] = vault;

    /// <summary>Register a synopsis source. Context provides IMemory and IDocumentSession at query time.</summary>
    public void Register<TResult, TQuery>(
        IViewStreamOrigin<TResult, TQuery, (IMemory Memory, IDocumentSession Session)> source)
        where TQuery : Query<TResult>
        => sources[(typeof(TResult), typeof(TQuery))] = source;

    /// <inheritdoc/>
    public IMemory Build() => new PostgresMemory(store, entities, vaults, sources);
}
