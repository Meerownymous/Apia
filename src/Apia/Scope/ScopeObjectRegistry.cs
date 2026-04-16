namespace Apia.Scope;

/// <summary>
/// Holds the <see cref="IEntitiesScope{TRecord,TFilter}"/> objects registered for a
/// specific filter type <typeparamref name="TFilter"/>.
/// Stored type-erased; cast back on retrieval.
/// </summary>
internal sealed class ScopeObjectRegistry<TFilter>
{
    private readonly Dictionary<Type, object> scopes = new();

    internal void Register<TRecord>(IEntitiesScope<TRecord, TFilter> scope)
        => scopes[typeof(TRecord)] = scope;

    internal bool HasScope<TRecord>()
        => scopes.ContainsKey(typeof(TRecord));

    internal IEntitiesScope<TRecord, TFilter> ScopeFor<TRecord>()
        => (IEntitiesScope<TRecord, TFilter>)scopes[typeof(TRecord)];
}
