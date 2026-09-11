using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>The units of work a Postgres memory hands out, each over a session of its own.</summary>
public sealed class PostgresBranches(IDocumentStore store, IIdentities identities, IOverrides overrides) : IBranches
{
    public IBranch Branch() => new PostgresBranch(store.LightweightSession(), identities, overrides, this);
}
