namespace Apia.Scope;

/// <summary>
/// A mutable collection of per-record access policies, built fluently and consumed by
/// <see cref="PolicyMemory{TContext}"/>. Replaces the former PolicyBuilder + PolicyRegistry pair.
///
/// <code>
/// IMemory userMemory = memory.WithPolicy(
///     currentUser,
///     p => p
///         .With&lt;Post&gt;(
///             read:   (post, ctx) => ctx.IsAdmin || post.AuthorId == ctx.Id,
///             write:  (post, ctx) => post.AuthorId == ctx.Id,
///             delete: (post, ctx) => post.AuthorId == ctx.Id || ctx.IsAdmin)
///         .With&lt;Comment&gt;((c, ctx) => c.AuthorId == ctx.Id)
/// );
/// </code>
/// </summary>
public sealed class Policies<TContext> : IPolicies<TContext>
{
    private readonly Dictionary<Type, object> store = new();

    /// <inheritdoc/>
    public bool Has<TRecord>() => store.ContainsKey(typeof(TRecord));

    /// <inheritdoc/>
    public IAccessPolicy<TRecord, TContext> Of<TRecord>()
        => (IAccessPolicy<TRecord, TContext>)store[typeof(TRecord)];

    /// <inheritdoc/>
    public IPolicies<TContext> With<TRecord>(
        Func<TRecord, TContext, bool> read,
        Func<TRecord, TContext, bool> write,
        Func<TRecord, TContext, bool> delete)
    {
        store[typeof(TRecord)] = new AccessPolicy<TRecord, TContext>(read, write, delete);
        return this;
    }

    /// <inheritdoc/>
    public IPolicies<TContext> With<TRecord>(Func<TRecord, TContext, bool> access)
        => With<TRecord>(access, access, access);
}
