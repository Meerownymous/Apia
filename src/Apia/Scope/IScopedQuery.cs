namespace Apia.Scope;

/// <summary>
/// Opt-in marker for queries that carry a policy context of type <typeparamref name="TContext"/>.
///
/// When a query implements this interface, <see cref="PolicyMemory{TContext}"/> automatically
/// injects the active context before forwarding to the backend.  The projection
/// (ISynopsisStream / ISynopsis) receives the full context and can apply
/// backend-native filtering — e.g. a Postgres synopsis adds a WHERE clause,
/// a RAM synopsis filters in-memory.
///
/// Implement via a record `with`-expression:
/// <code>
/// public record PostsByAuthorQuery(int Limit) : IQuery&lt;Post&gt;, IScopedQuery&lt;UserContext&gt;
/// {
///     public UserContext? Context { get; init; }
///
///     IScopedQuery&lt;UserContext&gt; IScopedQuery&lt;UserContext&gt;.WithContext(UserContext ctx)
///         => this with { Context = ctx };
/// }
/// </code>
///
/// If the query does NOT implement this interface, <see cref="PolicyMemory{TContext}"/>
/// falls back to post-filtering the result stream when a <c>canRead</c> predicate
/// is registered for the result type.
/// </summary>
public interface IScopedQuery<TContext>
{
    /// <summary>The context injected by <see cref="PolicyMemory{TContext}"/> at query time.</summary>
    TContext? Context { get; }

    /// <summary>Returns a copy of this query with the given context set.</summary>
    IScopedQuery<TContext> WithContext(TContext context);
}
