using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>Read-only Postgres vault. Opens a lightweight session per Load call.</summary>
public sealed class PostgresVault<T>(IDocumentStore store) : IVault<T>
{
    public async Task<OneOf<T, NotFound>> Load(string id)
    {
        await using var session = store.QuerySession();
        var record = await session.LoadAsync<T>(id);
        return record is null
            ? OneOf<T, NotFound>.FromT1(new NotFound())
            : OneOf<T, NotFound>.FromT0(record);
    }
}
