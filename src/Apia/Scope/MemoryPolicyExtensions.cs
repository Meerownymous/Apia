namespace Apia.Scope;

/// <summary>
/// Extension method that wraps any <see cref="IMemory"/> with a
/// <see cref="PolicyMemory{TContext}"/> configured via <see cref="IPolicies{TContext}"/>.
/// </summary>
public static class MemoryPolicyExtensions
{
    /// <summary>
    /// Returns a policy-enforced view of this memory for the given context.
    ///
    /// <code>
    /// // Same predicate for all operations:
    /// IMemory userMemory = memory.WithPolicy(
    ///     currentUser,
    ///     p => p
    ///         .With&lt;Post&gt;((post, ctx) => post.AuthorId == ctx.Id)
    ///         .With&lt;Comment&gt;((c, ctx) => c.AuthorId == ctx.Id)
    /// );
    ///
    /// // Different predicates per operation:
    /// IMemory boundMemory = memory.WithPolicy(
    ///     currentUser,
    ///     p => p.With&lt;Post&gt;(
    ///         read:   (post, ctx) => ctx.IsAdmin || post.AuthorId == ctx.Id,
    ///         write:  (post, ctx) => post.AuthorId == ctx.Id,
    ///         delete: (post, ctx) => post.AuthorId == ctx.Id || ctx.IsAdmin)
    /// );
    /// </code>
    ///
    /// Types not registered in <paramref name="configure"/> pass through to the
    /// inner backend unchanged — global or shared data remains fully accessible.
    /// </summary>
    /// <typeparam name="TContext">
    /// The context type — a user object, a plain <see cref="Guid"/>, or any value
    /// needed by the access predicates.
    /// </typeparam>
    /// <param name="memory">The backend memory to wrap.</param>
    /// <param name="context">The active context for this request or operation scope.</param>
    /// <param name="configure">Registers per-type access policies via fluent <c>With</c> calls.</param>
    public static IMemory WithPolicy<TContext>(
        this IMemory memory,
        TContext context,
        Action<IPolicies<TContext>> configure)
    {
        var policies = new Policies<TContext>();
        configure(policies);
        return new PolicyMemory<TContext>(memory, policies, context);
    }
}
