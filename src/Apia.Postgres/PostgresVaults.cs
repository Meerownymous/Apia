using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>The vaults of a Postgres memory, one per entity type.</summary>
public sealed class PostgresVaults(IDocumentStore store) : IVaults
{
    public IVault<T> Vault<T>() where T : notnull => new PostgresVault<T>(store);
}
