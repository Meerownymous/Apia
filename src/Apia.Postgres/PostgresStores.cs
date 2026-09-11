using Apia;
using Marten;

namespace Apia.Postgres;

/// <summary>The stores a Postgres branch writes through, all sharing the branch's session.</summary>
public sealed class PostgresStores(IDocumentSession session) : IStores
{
    public IEntityStore<T> Store<T>() where T : notnull => new PostgresEntityStore<T>(session);
}
