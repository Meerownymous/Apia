using OneOf;

namespace Apia.Scope;

/// <summary>
/// An <see cref="IEntities{TRecord}"/> decorator that enforces access policies on every operation.
///
/// <list type="bullet">
///   <item><term>All</term><description>Streams only records for which <c>CanRead</c> is true.</description></item>
///   <item><term>Load</term><description>Returns <see cref="NotFound"/> when <c>CanRead</c> is false —
///     indistinguishable from a genuine miss, preventing ID enumeration across scope boundaries.</description></item>
///   <item><term>Save</term><description>Throws <see cref="UnauthorizedAccessException"/> when <c>CanWrite</c> is false.</description></item>
///   <item><term>Delete</term><description>Loads the record first to evaluate <c>CanDelete</c>;
///     silently no-ops for genuinely missing records.</description></item>
/// </list>
/// </summary>
public sealed class PolicyEnforcedEntities<TRecord, TContext> : IEntities<TRecord>
{
    private readonly IEntities<TRecord> inner;
    private readonly TContext context;
    private readonly IAccessPolicy<TRecord, TContext> policy;

    /// <summary>Wraps <paramref name="inner"/> with the given context and access policy.</summary>
    public PolicyEnforcedEntities(
        IEntities<TRecord> inner,
        TContext context,
        IAccessPolicy<TRecord, TContext> policy)
    {
        this.inner   = inner;
        this.context = context;
        this.policy  = policy;
    }

    /// <inheritdoc/>
    public Guid IdOf(TRecord record) => inner.IdOf(record);

    /// <summary>Streams only records for which <c>CanRead(record, context)</c> is true.</summary>
    public async IAsyncEnumerable<TRecord> All()
    {
        await foreach (var record in inner.All())
            if (policy.CanRead(record, context))
                yield return record;
    }

    /// <summary>
    /// Returns <see cref="NotFound"/> when the record exists but <c>CanRead</c> denies access —
    /// indistinguishable from a genuine miss, preventing ID enumeration attacks.
    /// </summary>
    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var result = await inner.Load(id);
        return result.Match<OneOf<TRecord, NotFound>>(
            record => policy.CanRead(record, context) ? record : new NotFound(),
            notFound => notFound
        );
    }

    /// <summary>Rejects the save when <c>CanWrite(record, context)</c> is false.</summary>
    public Task<OneOf<TRecord, Conflict<TRecord>>> Save(TRecord record)
    {
        if (!policy.CanWrite(record, context))
            throw new UnauthorizedAccessException(
                $"Access denied: cannot save {typeof(TRecord).Name} in the current context.");

        return inner.Save(record);
    }

    /// <summary>
    /// Verifies <c>CanDelete</c> before delegating.
    /// Silently no-ops for genuinely missing records.
    /// </summary>
    public async Task Delete(Guid id)
    {
        var result = await inner.Load(id);
        var allowed = result.Match(
            record => policy.CanDelete(record, context),
            _ => true
        );

        if (!allowed)
            throw new UnauthorizedAccessException(
                $"Access denied: cannot delete {typeof(TRecord).Name} {id} in the current context.");

        await inner.Delete(id);
    }
}
