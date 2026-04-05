using Apia;
using Marten;

namespace Apia.Postgres;

public abstract class PostgresSynopsis<T, TQuery>(IDocumentSession session) : IProjection<T, TQuery>
    where TQuery : Query<T>
    where T : notnull
{
    //protected readonly IDocumentSession Session;

    //protected PostgresSynopsis(IDocumentSession session) => Session = session;

    public abstract IAsyncEnumerable<T> Query(TQuery query);
}
