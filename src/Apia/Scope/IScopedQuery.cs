namespace Apia.Scope;

/// <summary>
/// Opt-in marker for queries that carry a policy context of type <typeparamref name="TContext"/>.
///
/// When a query implements this interface, <see cref="PolicyMemory{TContext}"/> automatically
/// injects the active context before forwarding to the backend. The synopsis then receives
/// the context-bearing query and can apply backend-native filtering — e.g. a Postgres synopsis
/// adds a WHERE clause, a RAM synopsis filters in-memory.
///
/// Implement by inheriting <see cref="ScopedQuery{TSelf,TResult,TContext}"/>, which provides
/// the type-safe <c>WithContext</c> override and satisfies all constraints automatically:
/// <code>
/// public record PostsByAuthorQuery(int Limit) : ScopedQuery&lt;PostsByAuthorQuery, Post, UserContext&gt;
/// {
///     public UserContext? Context { get; init; }
///     public override PostsByAuthorQuery WithContext(UserContext ctx) =&gt; this with { Context = ctx };
/// }
/// </code>
///
/// If the query does NOT implement this interface, <see cref="PolicyMemory{TContext}"/>
/// falls back to post-filtering the result stream when a read predicate is registered
/// for the result type.
/// </summary>
public interface IScopedQuery<TContext>
{
    /// <summary>Returns a copy of this query with the given context injected.</summary>
    IScopedQuery<TContext> WithContext(TContext context);
}
