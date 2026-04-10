using OneOf;

namespace Apia.Scope;

/// <summary>
/// Decorator that enforces access policies on an existing <see cref="IEntities{TRecord}"/>.
///
/// Each operation applies the corresponding predicate from the registered
/// <see cref="TypePolicy{TRecord,TContext}"/>:
///
/// <list type="bullet">
///   <item><term>All</term><description>Streams only records for which <c>canRead</c> returns <see langword="true"/>.</description></item>
///   <item><term>Load</term><description>Returns <see cref="NotFound"/> when <c>canRead</c> is false — indistinguishable from a genuine miss, preventing ID enumeration across scope boundaries.</description></item>
///   <item><term>Save</term><description>Throws <see cref="UnauthorizedAccessException"/> when <c>canWrite</c> is false.</description></item>
///   <item><term>Delete</term><description>Loads the record first to evaluate <c>canDelete</c>; silently no-ops for genuinely missing records.</description></item>
/// </list>
///
/// The inner backend is called transparently — RAM keeps its ConcurrentDictionary,
/// Postgres keeps its Marten session, File keeps its JSON files.
/// No backend changes are needed.
/// </summary>
internal sealed class PolicyEnforcedEntities<TRecord, TContext> : IEntities<TRecord>
{
    private readonly IEntities<TRecord> inner;
    private readonly TContext context;
    private readonly TypePolicy<TRecord, TContext> policy;

    internal PolicyEnforcedEntities(
        IEntities<TRecord> inner,
        TContext context,
        TypePolicy<TRecord, TContext> policy)
    {
        this.inner   = inner;
        this.context = context;
        this.policy  = policy;
    }

    public Guid IdOf(TRecord record) => inner.IdOf(record);

    /// <summary>Streams only records for which <c>canRead(record, context)</c> is true.</summary>
    public async IAsyncEnumerable<TRecord> All()
    {
        await foreach (var record in inner.All())
            if (policy.CanRead(record, context))
                yield return record;
    }

    /// <summary>
    /// Returns <see cref="NotFound"/> when the record exists but <c>canRead</c> denies access.
    /// This is intentional: the caller cannot distinguish a missing record from an
    /// inaccessible one, preventing foreign-ID enumeration attacks.
    /// </summary>
    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var result = await inner.Load(id);
        return result.Match<OneOf<TRecord, NotFound>>(
            record => policy.CanRead(record, context)
                ? record
                : new NotFound(),
            notFound => notFound
        );
    }

    /// <summary>Rejects the save when <c>canWrite(record, context)</c> is false.</summary>
    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
        if (!policy.CanWrite(record, context))
            throw new UnauthorizedAccessException(
                $"Access denied: cannot save {typeof(TRecord).Name} with the current context.");

        return inner.Save(record);
    }

    /// <summary>
    /// Verifies <c>canDelete</c> before delegating.
    /// Silently no-ops for genuinely missing records (consistent with backend behaviour).
    /// </summary>
    public async Task Delete(Guid id)
    {
        var result = await inner.Load(id);
        var allowed = result.Match(
            record => policy.CanDelete(record, context),
            _ => true   // genuinely missing — Delete is a no-op anyway
        );

        if (!allowed)
            throw new UnauthorizedAccessException(
                $"Access denied: cannot delete {typeof(TRecord).Name} {id} with the current context.");

        await inner.Delete(id);
    }
}
