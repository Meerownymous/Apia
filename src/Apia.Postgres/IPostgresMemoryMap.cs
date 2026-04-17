using Apia;
using Marten;

namespace Apia.Postgres;

public interface IPostgresMemoryMap : IMemoryMap
{
    void RegisterQuery<T, TQuery>(Func<TQuery, IMemory, IDocumentSession, IAsyncEnumerable<T>> source) where T : notnull;
    void RegisterProjection<T, TQuery>(Func<TQuery, IMemory, IDocumentSession, Task<T>> source) where T : notnull;
}
