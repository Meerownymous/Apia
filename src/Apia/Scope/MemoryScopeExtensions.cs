namespace Apia.Scope;

/// <summary>
/// Extension that activates registered <see cref="IEntitiesScope{TRecord,TFilter}"/> objects
/// for a given filter value, returning a scope-enforced <see cref="IMemory"/>.
/// </summary>
public static class MemoryScopeExtensions
{
    /// <summary>
    /// Returns an <see cref="IMemory"/> where every registered scope is enforced
    /// against <paramref name="filter"/>.
    ///
    /// <code>
    /// // App startup — register scopes in the builder:
    /// var builder = new ScopeBuilder&lt;UserContext&gt;()
    ///     .Register&lt;Post&gt;(new UserPosts())
    ///     .Register&lt;Comment&gt;(new UserComments());
    ///
    /// // Per-request — activate with the current user:
    /// IMemory userMemory = memory.Scoped(currentUser, builder);
    ///
    /// // Use case receives IMemory, unaware of scope enforcement:
    /// var posts = await userMemory.Entities&lt;Post&gt;().All();
    /// </code>
    ///
    /// Record types not registered in <paramref name="builder"/> pass through unchanged.
    /// </summary>
    public static IMemory Scoped<TFilter>(
        this IMemory memory,
        TFilter filter,
        ScopeBuilder<TFilter> builder)
    {
        return new ScopeMemory<TFilter>(memory, builder.Build(), filter);
    }
}
