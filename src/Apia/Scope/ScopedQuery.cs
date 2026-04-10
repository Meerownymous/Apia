namespace Apia.Scope;

/// <summary>
/// Abstract base record for queries that carry a policy context of type
/// <typeparamref name="TContext"/>. Inherit from this instead of
/// <see cref="Query{TResult}"/> to enable transparent context injection by
/// <see cref="PolicyMemory{TContext}"/>.
///
/// <para>
/// The concrete record overrides <see cref="WithContext"/> and stores the injected
/// context in a property accessible to the synopsis:
/// </para>
/// <code>
/// public record PostsByAuthorQuery(int Limit)
///     : ScopedQuery&lt;PostsByAuthorQuery, Post, UserContext&gt;
/// {
///     public UserContext? Context { get; init; }
///     public override PostsByAuthorQuery WithContext(UserContext ctx) =&gt; this with { Context = ctx };
/// }
/// </code>
///
/// <para>
/// The synopsis receives the fully context-bearing query and accesses the context
/// through the concrete property — no casting required:
/// </para>
/// <code>
/// public sealed class PostsByAuthorStream : IViewStream&lt;Post, PostsByAuthorQuery&gt;
/// {
///     public async IAsyncEnumerable&lt;Post&gt; From(PostsByAuthorQuery seed)
///     {
///         var userId = seed.Context?.Id ?? throw new InvalidOperationException("Context required");
///         // ... filter by userId
///     }
/// }
/// </code>
/// </summary>
public abstract record ScopedQuery<TSelf, TResult, TContext> : Query<TResult>, IScopedQuery<TContext>
    where TSelf : ScopedQuery<TSelf, TResult, TContext>
{
    /// <summary>Returns a copy of this query with the given context injected.</summary>
    public abstract TSelf WithContext(TContext context);

    IScopedQuery<TContext> IScopedQuery<TContext>.WithContext(TContext context)
        => WithContext(context);
}
