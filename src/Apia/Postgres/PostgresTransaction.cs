using System.Collections.Concurrent;
using Apia;
using Marten;

namespace Apia.Postgres;

public sealed class PostgresTransaction(
    IDocumentSession session,
    ConcurrentDictionary<Type, object> entities,
    ConcurrentDictionary<Type, object> vaults,
    ConcurrentDictionary<(Type, Type), object> sources)
    : ITransaction
{
    private readonly PostgresTransactionMemory postgresTransactionMemory = new(session, entities, vaults, sources);

    public IMemory Memory() => postgresTransactionMemory;

    public async Task Commit() => await session.SaveChangesAsync();

    public async ValueTask DisposeAsync() => await session.DisposeAsync();
}