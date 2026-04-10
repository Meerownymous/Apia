namespace Apia.Scope;

/// <summary>
/// Fluent builder for registering access policies per record type.
/// Used inside <see cref="MemoryPolicyExtensions.WithPolicy{TContext}"/>.
///
/// <code>
/// IMemory restricted = memory.WithPolicy(
///     currentUser,
///     policy => policy
///         .For&lt;Post&gt;(
///             read:   (p, ctx) => ctx.IsAdmin || p.AuthorId == ctx.Id,
///             write:  (p, ctx) => p.AuthorId == ctx.Id,
///             delete: (p, ctx) => p.AuthorId == ctx.Id || ctx.IsAdmin
///         )
///         .For&lt;Comment&gt;(
///             read: (c, ctx) => ctx.IsAdmin || c.UserId == ctx.Id
///             // write and delete default to the same predicate as read
///         )
/// );
/// </code>
///
/// Record types not registered here pass through to the inner backend unchanged
/// — global / shared data remains fully accessible.
/// </summary>
public sealed class PolicyBuilder<TContext>
{
    private readonly PolicyRegistry<TContext> registry = new();

    /// <summary>
    /// Register access predicates for <typeparamref name="TRecord"/>.
    /// </summary>
    /// <param name="read">Controls <c>All()</c> and <c>Load()</c>.</param>
    /// <param name="write">Controls <c>Save()</c>. Defaults to <paramref name="read"/> if omitted.</param>
    /// <param name="delete">Controls <c>Delete()</c>. Defaults to <paramref name="read"/> if omitted.</param>
    public PolicyBuilder<TContext> For<TRecord>(
        Func<TRecord, TContext, bool> read,
        Func<TRecord, TContext, bool>? write  = null,
        Func<TRecord, TContext, bool>? delete = null)
    {
        registry.Register(new TypePolicy<TRecord, TContext>(
            CanRead:   read,
            CanWrite:  write  ?? read,
            CanDelete: delete ?? read
        ));
        return this;
    }

    internal PolicyRegistry<TContext> Build() => registry;
}
