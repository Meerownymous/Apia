using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>Read-only Postgres vault. Opens a lightweight session per Load call.</summary>
internal sealed class PostgresVault<T>(IDocumentStore store) : IVault<T>
{
    public async Task<OneOf<T, NotFound>> Load(Guid id)
    {
        await using var session = store.QuerySession();
        var record = await session.LoadAsync<T>(id);
        return record is null ? new NotFound() : record;
    }
}
