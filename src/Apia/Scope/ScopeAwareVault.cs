using OneOf;

namespace Apia.Scope;

/// <summary>
/// Wraps <see cref="IVault{T}"/> and hides records that fall outside the active scope.
/// Returns <see cref="NotFound"/> for out-of-scope ids — indistinguishable from a genuine miss.
/// </summary>
internal sealed class ScopeAwareVault<TRecord, TFilter>(
    IVault<TRecord> inner,
    IVaultScope<TRecord, TFilter> scope,
    TFilter filter)
    : IVault<TRecord>
{
    public async Task<OneOf<TRecord, NotFound>> Load(Guid id)
    {
        var result = await inner.Load(id);
        return result.Match<OneOf<TRecord, NotFound>>(
            record => scope.Includes(record, filter) ? record : new NotFound(),
            notFound => notFound
        );
    }
}
