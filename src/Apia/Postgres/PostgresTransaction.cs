using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresTransaction : ITransaction
{
    private readonly IDocumentSession session;
    private readonly PostgresTransactionalMemory _postgresTransactionalMemory;

    internal PostgresTransaction(
        IDocumentSession session,
        ConcurrentDictionary<Type, object> entities,
        ConcurrentDictionary<Type, object> vaults,
        ConcurrentDictionary<(Type, Type), object> sources)
    {
        this.session        = session;
        _postgresTransactionalMemory = new PostgresTransactionalMemory(session, entities, vaults, sources);
    }

    public IMemory Memory() => _postgresTransactionalMemory;

    public async Task Commit() => await session.SaveChangesAsync();

    public async ValueTask DisposeAsync() => await session.DisposeAsync();
}