using Apia;
using Marten;

namespace Apia.Postgres;

public abstract class PostgresSynopsis<T, TQuery>(IDocumentSession session) : IViews<T, TQuery>
    where TQuery : Query<T>
    where T : notnull
{
    public abstract IAsyncEnumerable<T> Query(TQuery query);
}
