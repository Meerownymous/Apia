using Apia;
using Marten;

namespace Apia.Postgres;

public abstract class PostgresSynopsis<T, TQuery>(IDocumentSession session) : IViewStream<T, TQuery>
    where TQuery : QueryRecord<T>
    where T : notnull
{
    public abstract IAsyncEnumerable<T> Query(TQuery query);
}
