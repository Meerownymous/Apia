using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>
/// A memory holding its entities in Postgres through Marten. Moving to SQL is a change of composition
/// and nothing else: the identities are the same ones every other backend is composed from.
/// <para>
/// Marten decides for itself which member of an entity carries its id. The document store handed in
/// here must be configured so that it agrees with the identities — Apia does not configure Marten.
/// </para>
/// <para>
/// Commit atomicity: a commit is one Marten transaction across every entity type it touches, so it
/// either happens entirely or not at all.
/// </para>
/// </summary>
public sealed class PostgresMemory : IMemory
{
    private readonly IMemory memory;

    public PostgresMemory(IDocumentStore store, IIdentities identities, IOverrides overrides)
        => memory = new Memory(
            new PostgresVaults(store),
            new PostgresBranches(store, identities, overrides),
            overrides);

    public IAsyncEnumerable<T> Aggregate<T>(IAggregateQuery<T> query) where T : notnull => memory.Aggregate(query);

    public Task<T> Projection<T>(IProjectionQuery<T> query) where T : notnull => memory.Projection(query);

    public IVault<T> Vault<T>() where T : notnull => memory.Vault<T>();

    public IBranch Branch() => memory.Branch();
}
