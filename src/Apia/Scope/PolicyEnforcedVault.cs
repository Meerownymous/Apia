using OneOf;

namespace Apia.Scope;

/// <summary>
/// Wraps <see cref="IVault{T}"/> and hides records for which <c>CanRead</c> is false.
/// Returns <see cref="NotFound"/> for denied ids — indistinguishable from a genuine miss.
/// </summary>
internal sealed class PolicyEnforcedVault<TRecord, TContext>(
    IVault<TRecord> inner,
    TContext context,
    IAccessPolicy<TRecord, TContext> policy)
    : IVault<TRecord>
{
    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var result = await inner.Load(id);
        return result.Match<OneOf<TRecord, NotFound>>(
            record => policy.CanRead(record, context) ? record : new NotFound(),
            notFound => notFound
        );
    }
}
