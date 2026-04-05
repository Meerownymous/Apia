using System.Collections.Concurrent;
using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// Postgres-backed catalog. Holds only the id selector — session is bound at query time.
/// Usage: new PostgresEntities&lt;PostRecord&gt;(p => p.PostId)
/// </summary>
public sealed class PostgresEntities<TResult>(Func<TResult, Guid> idOf) : IEntities<TResult>
    where TResult : notnull
{
    internal BoundPostgresEntities<TResult> Bind(IDocumentSession session)
        => new(session, idOf);

    public Guid IdOf(TResult record)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public IAsyncEnumerable<TResult> All()
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task<OneOf<TResult, NotFound>> Load(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task Delete(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");
}