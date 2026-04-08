using OneOf;

namespace Apia.Postgres;

/// <summary>
/// Postgres-backed catalog registration holder. Holds only the id selector — session is bound at query time.
/// Usage: new PostgresEntities&lt;PostRecord&gt;(p => p.PostId)
/// </summary>
public sealed class PostgresEntities<TResult>(Func<TResult, Guid> idOf) : IEntities<TResult>
    where TResult : notnull
{
    public Guid IdOf(TResult record) => idOf(record);

    public IAsyncEnumerable<TResult> All()
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task<OneOf<TResult, NotFound>> Load(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task<OneOf<TResult, Conflict<TResult>>> Save(TResult record)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");

    public Task Delete(Guid id)
        => throw new InvalidOperationException("Use IMemory.Entities<T>() to access this store.");
}
