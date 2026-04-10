namespace Apia.Scope;

/// <summary>
/// Extension method that wraps any <see cref="IMemory"/> with a
/// <see cref="PolicyMemory{TContext}"/> configured via <see cref="PolicyBuilder{TContext}"/>.
/// </summary>
public static class MemoryPolicyExtensions
{
    /// <summary>
    /// Returns a policy-enforced view of this memory.
    ///
    /// <code>
    /// // Simple ownership (one predicate for all operations):
    /// IMemory userMemory = memory.WithPolicy(
    ///     currentUser.Id,
    ///     policy => policy
    ///         .For&lt;Post&gt;(read: (p, userId) => p.AuthorId == userId)
    ///         .For&lt;Comment&gt;(read: (c, userId) => c.AuthorId == userId)
    /// );
    ///
    /// // Role-based (different predicates per operation):
    /// IMemory boundMemory = memory.WithPolicy(
    ///     currentUser,
    ///     policy => policy
    ///         .For&lt;Post&gt;(
    ///             read:   (p, ctx) => ctx.IsAdmin || p.AuthorId == ctx.Id,
    ///             write:  (p, ctx) => p.AuthorId == ctx.Id,
    ///             delete: (p, ctx) => p.AuthorId == ctx.Id || ctx.IsAdmin
    ///         )
    /// );
    /// </code>
    ///
    /// Types not registered in <paramref name="configure"/> pass through to the
    /// inner backend unchanged — global / shared data remains fully accessible.
    /// </summary>
    /// <typeparam name="TContext">
    /// The context type. Can be a plain <see cref="Guid"/> (user/tenant/project ID),
    /// a user object with roles, or any other value needed by the predicates.
    /// </typeparam>
    /// <param name="memory">The backend memory to wrap.</param>
    /// <param name="context">The active context for this request / operation scope.</param>
    /// <param name="configure">Registers per-type access policies.</param>
    public static IMemory WithPolicy<TContext>(
        this IMemory memory,
        TContext context,
        Action<PolicyBuilder<TContext>> configure)
    {
        var builder = new PolicyBuilder<TContext>();
        configure(builder);
        return new PolicyMemory<TContext>(memory, builder.Build(), context);
    }
}
