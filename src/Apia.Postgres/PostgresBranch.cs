using Apia;
using Marten;
using OneOf;

namespace Apia.Postgres;

/// <summary>
/// A unit of work over one Marten session. Staged changes reach the session on commit and the database
/// in the one transaction the session saves in, so a commit on this backend is all or nothing. The
/// session is closed when the branch commits, whatever the outcome.
/// </summary>
public sealed class PostgresBranch : IBranch
{
    private readonly IDocumentSession session;
    private readonly IBranch inner;

    public PostgresBranch(IDocumentSession session, IIdentities identities, IOverrides overrides, IBranches branches)
    {
        this.session = session;
        inner = new StoreBranch(new PostgresStores(session), identities, overrides, branches);
    }

    public IMemory Memory() => inner.Memory();

    public Task Save<T>(T entity) where T : notnull => inner.Save(entity);

    public Task Delete<T>(Guid id) where T : notnull => inner.Delete<T>(id);

    public async Task<OneOf<Committed, Stale>> Commit()
    {
        try
        {
            var outcome = await inner.Commit();
            await outcome.Match(_ => session.SaveChangesAsync(), _ => Task.CompletedTask);
            return outcome;
        }
        finally { await session.DisposeAsync(); }
    }
}
