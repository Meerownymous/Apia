namespace Apia.Scope;

/// <summary>
/// Internal record holding the three access predicates for one record type.
/// Built by <see cref="PolicyBuilder{TContext}"/>, consumed by <see cref="PolicyMemory{TContext}"/>.
/// </summary>
internal sealed record TypePolicy<TRecord, TContext>(
    Func<TRecord, TContext, bool> CanRead,
    Func<TRecord, TContext, bool> CanWrite,
    Func<TRecord, TContext, bool> CanDelete
);

/// <summary>
/// Holds the per-type access policies registered via <see cref="PolicyBuilder{TContext}"/>.
/// Stored as <see langword="object"/> (type-erased) and cast back on retrieval.
/// </summary>
internal sealed class PolicyRegistry<TContext>
{
    private readonly Dictionary<Type, object> policies = new();

    internal void Register<TRecord>(TypePolicy<TRecord, TContext> policy)
        => policies[typeof(TRecord)] = policy;

    internal bool HasPolicy<TRecord>()
        => policies.ContainsKey(typeof(TRecord));

    internal TypePolicy<TRecord, TContext> PolicyFor<TRecord>()
        => (TypePolicy<TRecord, TContext>)policies[typeof(TRecord)];
}
